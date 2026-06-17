using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers;

[Area("Patient")]
[Authorize(Roles = "Patient")]
public class SubscriptionController : Controller
{
    private static readonly List<SubscriptionPlanViewModel> Plans =
    [
        new() { PlanId = "basic-monthly", Name = "Basic Bulanan", Price = 99000, DurationMonths = 1, Description = "Mood tracking, jurnal, dan statistik dasar." },
        new() { PlanId = "premium-monthly", Name = "Premium Bulanan", Price = 199000, DurationMonths = 1, Description = "Semua fitur Basic + prioritas worksheet dan laporan lanjutan." },
        new() { PlanId = "premium-yearly", Name = "Premium Tahunan", Price = 1990000, DurationMonths = 12, Description = "Premium 12 bulan (hemat ~17%)." }
    ];

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DuitkuService _duitku;
    private readonly SubscriptionAccessService _access;

    public SubscriptionController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        DuitkuService duitku,
        SubscriptionAccessService access)
    {
        _context = context;
        _userManager = userManager;
        _duitku = duitku;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var patient = await GetPatientAsync();
        if (patient == null) return RedirectToAction("Welcome", "Onboarding");

        var active = await _context.Subscriptions
            .Where(s => s.PatientId == patient.PatientId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        var companySponsors = patient.CompanyId != null
            && await _access.HasCompanyActiveSubscriptionAsync(patient.CompanyId.Value);

        string? companyName = null;
        if (patient.CompanyId != null)
            companyName = await _context.Companies.Where(c => c.CompanyId == patient.CompanyId)
                .Select(c => c.Name).FirstOrDefaultAsync();

        ViewBag.ActiveNav = "Langganan";
        return View(new PatientSubscriptionIndexViewModel
        {
            Plans = Plans,
            HasActiveSubscription = active != null || companySponsors,
            ActivePlanName = companySponsors ? "Ditanggung perusahaan" : active?.PlanName,
            ActiveUntil = companySponsors
                ? (await _access.GetActiveCompanySubscriptionAsync(patient.CompanyId!.Value))?.EndDate
                : active?.EndDate,
            IsB2B = patient.CompanyId != null,
            CompanySponsors = companySponsors,
            CompanyName = companyName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string planId)
    {
        var user = await _userManager.GetUserAsync(User);
        var patient = await GetPatientAsync();
        if (user == null || patient == null) return Unauthorized();

        var plan = Plans.FirstOrDefault(p => p.PlanId == planId);
        if (plan == null)
        {
            TempData["error"] = "Paket tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        if (patient.CompanyId != null && await _access.HasCompanyActiveSubscriptionAsync(patient.CompanyId.Value))
        {
            TempData["success"] = "Langganan perusahaan Anda masih aktif — tidak perlu bayar mandiri.";
            return RedirectToAction(nameof(Index));
        }

        var subscription = new Subscription
        {
            PatientId = patient.PatientId,
            PlanName = plan.Name,
            Status = "Pending",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(plan.DurationMonths)
        };
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var orderId = $"LU-{patient.PatientId}-{subscription.SubscriptionId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var payment = new PaymentTransaction
        {
            PatientId = patient.PatientId,
            SubscriptionId = subscription.SubscriptionId,
            MerchantOrderId = orderId,
            Amount = plan.Price,
            PlanName = plan.Name,
            PaymentStatus = "pending"
        };
        _context.PaymentTransactions.Add(payment);
        await _context.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var callbackUrl = $"{baseUrl}/dk/cb";
        var returnUrl = Url.Action(nameof(Return), "Subscription", new { area = "Patient", orderId }, Request.Scheme)!;

        var result = await _duitku.CreatePaymentAsync(new DuitkuCreatePaymentRequest
        {
            MerchantOrderId = orderId,
            Amount = plan.Price,
            ProductDetails = $"LightenUp {plan.Name}",
            CustomerEmail = user.Email ?? "",
            CustomerName = user.FullName,
            CallbackUrl = callbackUrl,
            ReturnUrl = returnUrl
        });

        if (!result.Success || string.IsNullOrEmpty(result.PaymentUrl))
        {
            TempData["error"] = result.ErrorMessage ?? "Gagal membuat pembayaran.";
            return RedirectToAction(nameof(Index));
        }

        payment.DuitkuReference = result.Reference;
        payment.PaymentUrl = result.PaymentUrl;
        await _context.SaveChangesAsync();

        return Redirect(result.PaymentUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Return(string orderId, bool mock = false)
    {
        var payment = await _context.PaymentTransactions
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.MerchantOrderId == orderId);

        if (payment == null) return NotFound();

        if (mock && payment.PaymentStatus == "pending")
            await PaymentCompletionService.MarkPaidAsync(_context, payment);

        if (payment.PaymentStatus == "paid")
            return RedirectToAction(nameof(Success), new { orderId });

        return RedirectToAction(nameof(Failed), new { orderId });
    }

    [HttpGet]
    public async Task<IActionResult> Success(string orderId)
    {
        var payment = await _context.PaymentTransactions.FirstOrDefaultAsync(p => p.MerchantOrderId == orderId);
        if (payment == null) return NotFound();
        ViewBag.PlanName = payment.PlanName;
        return View();
    }

    [HttpGet]
    public IActionResult Failed(string orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    private async Task<LightenUp.Web.Models.Patient?> GetPatientAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;
        return await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
    }
}
