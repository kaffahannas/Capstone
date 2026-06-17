using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers;

[Area("Hr")]
[Authorize(Roles = "HR")]
public class SubscriptionController : Controller
{
    private static readonly List<SubscriptionPlanViewModel> Plans =
    [
        new() { PlanId = "company-test", Name = "Paket Test (Rp 10.000)", Price = 10000, DurationMonths = 1, EmployeeLimit = 1, Description = "Paket khusus simulasi pembayaran. 1 slot karyawan, durasi 1 bulan." },
        new() { PlanId = "company-10", Name = "Perusahaan 10 Karyawan", Price = 4500000, DurationMonths = 12, EmployeeLimit = 10, Description = "Hingga 10 karyawan terdaftar. Nilai slot payroll: Rp 450.000/karyawan/bulan." },
        new() { PlanId = "company-25", Name = "Perusahaan 25 Karyawan", Price = 10000000, DurationMonths = 12, EmployeeLimit = 25, Description = "Hingga 25 karyawan terdaftar. Nilai slot payroll: Rp 400.000/karyawan/bulan." },
        new() { PlanId = "company-50", Name = "Perusahaan 50 Karyawan", Price = 18000000, DurationMonths = 12, EmployeeLimit = 50, Description = "Hingga 50 karyawan terdaftar. Nilai slot payroll: Rp 360.000/karyawan/bulan." }
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
        var hr = await GetHrAsync();
        if (hr == null || hr.OnboardingCompletedAt == null)
            return RedirectToAction("Welcome", "Onboarding");

        CompanySubscription? active = null;
        int employeeCount = 0;
        int employeeLimit = 0;
        if (hr.CompanyId != null)
        {
            active = await _access.GetActiveCompanySubscriptionAsync(hr.CompanyId.Value);
            employeeCount = await _context.Patients.CountAsync(p =>
                p.CompanyId == hr.CompanyId && p.EmploymentStatus == "active");
            employeeCount += await _context.PendingEmployees.CountAsync(p => p.CompanyId == hr.CompanyId);
            employeeLimit = active?.EmployeeLimit ?? 0;
        }

        var divisions = new List<CompanyDivisionViewModel>();
        if (hr.CompanyId != null)
        {
            divisions = await _context.CompanyDivisions
                .Where(d => d.CompanyId == hr.CompanyId)
                .Select(d => new CompanyDivisionViewModel
                {
                    DivisionId = d.DivisionId,
                    Name = d.Name,
                    ReferralCode = d.ReferralCode,
                    EmployeeCount = _context.Patients.Count(p => p.CompanyId == hr.CompanyId && p.DivisionId == d.DivisionId) +
                                    _context.PendingEmployees.Count(p => p.CompanyId == hr.CompanyId && p.DivisionId == d.DivisionId)
                })
                .ToListAsync();
        }

        ViewBag.ActiveNav = "Langganan";
        ViewBag.EmployeeCount = employeeCount;
        ViewBag.EmployeeLimit = employeeLimit;
        return View(new HrSubscriptionIndexViewModel
        {
            Plans = Plans,
            HasActiveSubscription = active != null,
            ActivePlanName = active?.PlanName,
            ActiveUntil = active?.EndDate,
            Divisions = divisions,
            CompanyName = hr.Company?.Name ?? "",
            SubscriptionRequired = TempData["subscriptionRequired"] != null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDivision(string name)
    {
        var hr = await GetHrAsync();
        if (hr?.CompanyId == null || string.IsNullOrWhiteSpace(name))
            return RedirectToAction(nameof(Index));

        var newDiv = new CompanyDivision
        {
            CompanyId = hr.CompanyId.Value,
            Name = name.Trim(),
            ReferralCode = await _access.GenerateUniqueReferralCodeAsync()
        };
        _context.CompanyDivisions.Add(newDiv);
        await _context.SaveChangesAsync();

        TempData["success"] = "Divisi berhasil ditambahkan!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDivision(int id)
    {
        var hr = await GetHrAsync();
        if (hr?.CompanyId == null) return RedirectToAction(nameof(Index));

        var div = await _context.CompanyDivisions.FirstOrDefaultAsync(d => d.DivisionId == id && d.CompanyId == hr.CompanyId);
        if (div != null)
        {
            _context.CompanyDivisions.Remove(div);
            await _context.SaveChangesAsync();
            TempData["success"] = "Divisi berhasil dihapus.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string planId)
    {
        var user = await _userManager.GetUserAsync(User);
        var hr = await GetHrAsync();
        if (user == null || hr?.CompanyId == null) return Unauthorized();

        var plan = Plans.FirstOrDefault(p => p.PlanId == planId);
        if (plan == null)
        {
            TempData["error"] = "Paket tidak valid.";
            return RedirectToAction(nameof(Index));
        }

        var subscription = new CompanySubscription
        {
            CompanyId = hr.CompanyId.Value,
            PlanName = plan.Name,
            Status = "Pending",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(plan.DurationMonths),
            EmployeeLimit = plan.EmployeeLimit
        };
        _context.CompanySubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var orderId = $"LU-C{hr.CompanyId}-{subscription.CompanySubscriptionId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var payment = new PaymentTransaction
        {
            CompanyId = hr.CompanyId,
            CompanySubscriptionId = subscription.CompanySubscriptionId,
            MerchantOrderId = orderId,
            Amount = plan.Price,
            PlanName = plan.Name,
            PaymentStatus = "pending"
        };
        _context.PaymentTransactions.Add(payment);
        await _context.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var callbackUrl = $"{baseUrl}/dk/cb";
        var returnUrl = Url.Action(nameof(Return), "Subscription", new { area = "Hr", orderId }, Request.Scheme)!;

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
            .Include(p => p.CompanySubscription)
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
        var payment = await _context.PaymentTransactions
            .Include(p => p.Company)
            .FirstOrDefaultAsync(p => p.MerchantOrderId == orderId);
        if (payment == null) return NotFound();

        var company = payment.CompanyId != null
            ? await _context.Companies.FindAsync(payment.CompanyId)
            : null;
        ViewBag.PlanName = payment.PlanName;
        ViewBag.ActiveNav = "Langganan";
        return View();
    }

    [HttpGet]
    public IActionResult Failed(string orderId)
    {
        ViewBag.OrderId = orderId;
        ViewBag.ActiveNav = "Langganan";
        return View();
    }

    private async Task<HrStaff?> GetHrAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;
        return await _context.HrStaffs
            .Include(h => h.Company)
            .FirstOrDefaultAsync(h => h.UserId == user.Id);
    }
}
