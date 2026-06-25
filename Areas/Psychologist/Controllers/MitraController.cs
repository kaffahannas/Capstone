using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    [Authorize(Roles = "Psychologist")]
    public class MitraController : Controller
    {
        private static readonly List<MitraPlanViewModel> Plans =
        [
            new() { PlanId = "mitra-test", Name = "Paket Test",     Price = 10_000,  DurationMonths = 1, PatientLimit = 1,  Description = "Simulasi pembayaran. 1 klien klinik, durasi 1 bulan." },
            new() { PlanId = "mitra-10",   Name = "Mitra 10 Klien", Price = 199_000, DurationMonths = 1, PatientLimit = 10, Description = "Hingga 10 pasien klinik per bulan." },
            new() { PlanId = "mitra-25",   Name = "Mitra 25 Klien", Price = 399_000, DurationMonths = 1, PatientLimit = 25, Description = "Hingga 25 pasien klinik per bulan. Pilihan terbaik." },
            new() { PlanId = "mitra-50",   Name = "Mitra 50 Klien", Price = 699_000, DurationMonths = 1, PatientLimit = 50, Description = "Hingga 50 pasien klinik per bulan." },
        ];

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionAccessService _access;
        private readonly DuitkuService _duitku;

        public MitraController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SubscriptionAccessService access, DuitkuService duitku)
        {
            _context = context;
            _userManager = userManager;
            _access = access;
            _duitku = duitku;
        }

        private async Task<LightenUp.Web.Models.Psychologist?> GetPsyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ─── Halaman utama Mitra — status langganan + kode referal ───
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var activeSub = await _access.GetActivePsychologistSubscriptionAsync(psy.PsychologistId);

            var mitraPatients = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Assignments.Where(a => a.Status == "Active" && a.PsychologistId == psy.PsychologistId))
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId && p.SponsorType == "Psychologist")
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            var clientCount = mitraPatients.Count;

            ViewBag.Psy = psy;
            ViewBag.ActiveSub = activeSub;
            ViewBag.ClientCount = clientCount;
            ViewBag.MitraPatients = mitraPatients;
            ViewBag.IsMitraActive = activeSub != null;
            ViewBag.Plans = Plans;
            ViewBag.ActiveNav = "Mitra";
            ViewData["Title"] = "Mitra LightenUp";

            if (TempData["mitraRequired"] != null)
                TempData["info"] = "Aktifkan add-on Mitra untuk mengakses fitur monitoring klien klinik.";

            return View();
        }

        // ─── Generate kode referal baru ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPsychologistMitra]
        public async Task<IActionResult> GenerateReferralCode()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return NotFound();

            psy.MitraReferralCode = await _access.GenerateUniqueReferralCodeAsync();
            await _context.SaveChangesAsync();

            TempData["success"] = $"Kode referal baru: {psy.MitraReferralCode}";
            return RedirectToAction(nameof(Index));
        }

        // ─── Checkout Mitra ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string planId)
        {
            var user = await _userManager.GetUserAsync(User);
            var psy = await GetPsyAsync();
            if (user == null || psy == null) return Unauthorized();

            var plan = Plans.FirstOrDefault(p => p.PlanId == planId);
            if (plan == null)
            {
                TempData["error"] = "Paket tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var subscription = new PsychologistSubscription
            {
                PsychologistId = psy.PsychologistId,
                PlanName = plan.Name,
                Status = "Pending",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(plan.DurationMonths),
                PatientLimit = plan.PatientLimit
            };
            _context.PsychologistSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var orderId = $"LU-M{psy.PsychologistId}-{subscription.PsychologistSubscriptionId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var payment = new PaymentTransaction
            {
                PsychologistSubscriptionId = subscription.PsychologistSubscriptionId,
                MerchantOrderId = orderId,
                Amount = plan.Price,
                PlanName = plan.Name,
                PaymentStatus = "pending"
            };
            _context.PaymentTransactions.Add(payment);
            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var callbackUrl = $"{baseUrl}/dk/cb";
            var returnUrl = Url.Action(nameof(Return), "Mitra", new { area = "Psychologist", orderId }, Request.Scheme)!;

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

        // ─── Return setelah pembayaran ───
        [HttpGet]
        public async Task<IActionResult> Return(string orderId, bool mock = false)
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.PsychologistSubscription)
                .FirstOrDefaultAsync(p => p.MerchantOrderId == orderId);

            if (payment == null) return NotFound();

            if (mock && payment.PaymentStatus == "pending")
                await PaymentCompletionService.MarkPaidAsync(_context, payment);

            ViewBag.ActiveNav = "Mitra";
            if (payment.PaymentStatus == "paid")
            {
                TempData["success"] = $"Pembayaran berhasil! Paket {payment.PlanName} kini aktif.";
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = "Pembayaran belum terkonfirmasi. Coba beberapa saat lagi atau hubungi support.";
            return RedirectToAction(nameof(Index));
        }
    }
}
