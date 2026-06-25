using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PsychologistModel = LightenUp.Web.Models.Psychologist;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class PsychologistsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AssignmentActivationService _activation;
        private readonly DuitkuService _duitku;
        private readonly IWebHostEnvironment _env;

        public PsychologistsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AssignmentActivationService activation,
            DuitkuService duitku,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _activation = activation;
            _duitku = duitku;
            _env = env;
        }

        // ─── Daftar psikolog — terbuka untuk semua pasien ───
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients
                .Include(p => p.Company).ThenInclude(c => c!.PartneredPsychologists)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return RedirectToAction("Welcome", "Onboarding", new { area = "Patient" });

            var activeAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");

            bool hasActivePsychologist = false;
            if (activeAssignment != null)
            {
                if (patient.CompanyId != null)
                {
                    // B2B: akses tergantung company subscription
                    hasActivePsychologist = await _context.CompanySubscriptions
                        .AnyAsync(s => s.CompanyId == patient.CompanyId && s.Status == "Active" && s.EndDate >= DateTime.Today);
                }
                else if (patient.SponsorType == "Psychologist" && patient.SponsorPsychologistId != null)
                {
                    // Mitra psikolog: akses tergantung PsychologistSubscription sponsor
                    bool mitraActive = await _context.PsychologistSubscriptions
                        .AnyAsync(s => s.PsychologistId == patient.SponsorPsychologistId
                            && s.Status == "Active" && s.EndDate >= DateTime.Today);

                    if (!mitraActive)
                    {
                        // Lazy cancel
                        activeAssignment.Status = "Cancelled";
                        activeAssignment.CancellationReason = "Mitra subscription expired";
                        activeAssignment.CancellationRequestedAt = DateTime.UtcNow;
                        patient.SponsorType = "Self";
                        patient.SponsorPsychologistId = null;
                        await _context.SaveChangesAsync();
                    }

                    hasActivePsychologist = mitraActive;
                }
                else
                {
                    // B2C: akses tergantung personal subscription
                    hasActivePsychologist = await _context.Subscriptions
                        .AnyAsync(s => s.PatientId == patient.PatientId
                            && s.PsychologistId == activeAssignment.PsychologistId
                            && s.Status == "Active" && s.EndDate >= DateTime.Today);
                }
            }

            IQueryable<PsychologistModel> query = _context.Psychologists
                .Include(p => p.User)
                .Where(p => p.User != null && p.User.IsApprovedByAdmin);

            // B2B: hanya tampilkan panel mitra perusahaan
            if (patient.CompanyId != null && patient.Company?.PartneredPsychologists != null)
            {
                var panelIds = patient.Company.PartneredPsychologists.Select(p => p.PsychologistId).ToHashSet();
                query = query.Where(p => panelIds.Contains(p.PsychologistId));
            }

            var psychologists = await query.OrderBy(p => p.User!.FullName).ToListAsync();

            ViewBag.HasActivePsychologist = hasActivePsychologist;
            ViewBag.ActivePsychologistId = activeAssignment?.PsychologistId;
            ViewBag.PatientId = patient.PatientId;
            ViewBag.IsB2B = patient.CompanyId != null;
            ViewBag.ActiveNav = "Psikolog";
            ViewData["Title"] = "Cari Psikolog";
            return View(psychologists);
        }

        // ─── Pasien beli/pilih psikolog → langsung Active ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyPsychologist(int psychologistId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients
                .Include(p => p.Company).ThenInclude(c => c!.PartneredPsychologists)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            var psy = await _context.Psychologists.FindAsync(psychologistId);
            if (psy == null) return NotFound();

            // Cegah duplikat jika masih dalam proses pembatalan
            var pendingCancel = await _context.Assignments.AnyAsync(a =>
                a.PatientId == patient.PatientId &&
                (a.Status == "PendingCancellation" || a.Status == "PendingCancellationByHr"));
            if (pendingCancel)
            {
                TempData["error"] = "Terdapat permintaan pembatalan yang sedang diproses.";
                return RedirectToAction(nameof(Index));
            }

            // Cek apakah ada assignment aktif
            var existingAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (existingAssignment != null)
            {
                // B2C: izinkan beli baru jika subscription sudah expired (ganti psikolog)
                bool subStillActive = patient.CompanyId != null
                    ? await _context.CompanySubscriptions.AnyAsync(s =>
                        s.CompanyId == patient.CompanyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    : await _context.Subscriptions.AnyAsync(s =>
                        s.PatientId == patient.PatientId
                        && s.PsychologistId == existingAssignment.PsychologistId
                        && s.Status == "Active" && s.EndDate >= DateTime.Today);

                if (subStillActive)
                {
                    TempData["error"] = "Anda sudah memiliki psikolog aktif.";
                    return RedirectToAction(nameof(Index));
                }

                // Subscription expired — tutup assignment lama
                existingAssignment.Status = "Cancelled";
                existingAssignment.CancellationReason = "Subscription expired";
                existingAssignment.CancellationRequestedAt = DateTime.UtcNow;

                // Jika mitra psikolog expired, clear sponsor agar jadi B2C
                if (patient.SponsorType == "Psychologist")
                {
                    patient.SponsorType = "Self";
                    patient.SponsorPsychologistId = null;
                }
            }

            // B2B: pastikan psikolog ada di panel mitra
            if (patient.CompanyId != null)
            {
                var inPanel = patient.Company?.PartneredPsychologists.Any(p => p.PsychologistId == psychologistId) ?? false;
                if (!inPanel)
                {
                    TempData["error"] = "Psikolog tidak tersedia dalam panel perusahaan Anda.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (patient.SponsorType == "Psychologist" && patient.SponsorPsychologistId != null)
            {
                // Mitra klinik: harus sesuai dengan sponsor
                if (patient.SponsorPsychologistId != psychologistId)
                {
                    TempData["error"] = "Anda hanya dapat memilih psikolog klinik Anda.";
                    return RedirectToAction(nameof(Index));
                }

                // Cek PatientLimit dari Mitra subscription aktif
                var mitraSub = await _context.PsychologistSubscriptions
                    .Where(s => s.PsychologistId == psychologistId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();
                if (mitraSub != null && mitraSub.PatientLimit > 0)
                {
                    var currentKlienCount = await _context.Patients.CountAsync(p =>
                        p.SponsorPsychologistId == psychologistId &&
                        p.SponsorType == "Psychologist" &&
                        _context.Assignments.Any(a => a.PatientId == p.PatientId && a.Status == "Active"));
                    if (currentKlienCount >= mitraSub.PatientLimit)
                    {
                        TempData["error"] = $"Psikolog ini sudah mencapai batas maksimal klien klinik ({mitraSub.PatientLimit} klien).";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            else
            {
                // B2C: arahkan ke pembayaran Duitku
                if (psy.PricePerMonth == null || psy.PricePerMonth <= 0)
                {
                    TempData["error"] = "Psikolog ini belum mengatur harga layanan.";
                    return RedirectToAction(nameof(Index));
                }

                var subscription = new Subscription
                {
                    PatientId = patient.PatientId,
                    PsychologistId = psychologistId,
                    PlanName = "1-on-1 Counseling",
                    Status = "Pending",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(1),
                    MaxSessionsPerMonth = psy.SessionTokensPerMonth > 0 ? psy.SessionTokensPerMonth : 4
                };
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                var orderId = $"LU-B2C-{patient.PatientId}-{subscription.SubscriptionId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var payment = new PaymentTransaction
                {
                    PatientId = patient.PatientId,
                    SubscriptionId = subscription.SubscriptionId,
                    MerchantOrderId = orderId,
                    Amount = psy.PricePerMonth.Value,
                    PlanName = $"Konseling 1-on-1 — {psy.User?.FullName ?? "Psikolog"}",
                    PaymentStatus = "pending"
                };
                _context.PaymentTransactions.Add(payment);
                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var returnUrl = Url.Action(nameof(ReturnB2C), "Psychologists",
                    new { area = "Patient", orderId }, Request.Scheme)!;

                var result = await _duitku.CreatePaymentAsync(new DuitkuCreatePaymentRequest
                {
                    MerchantOrderId = orderId,
                    Amount = psy.PricePerMonth.Value,
                    ProductDetails = payment.PlanName,
                    CustomerEmail = user.Email ?? "",
                    CustomerName = user.FullName,
                    CallbackUrl = $"{baseUrl}/dk/cb",
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

            // B2B dan Klien Klinik: Langsung Active — tidak perlu approval
            var assignment = new PatientPsychologistAssignment
            {
                PatientId = patient.PatientId,
                PsychologistId = psychologistId,
                Status = "Active",
                AssignedAt = DateTime.UtcNow,
                RequestedByUserId = user.Id,
                RequestedByRole = "Patient"
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["success"] = "Psikolog berhasil dipilih. Anda sekarang bisa menjadwalkan sesi.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Return setelah pembayaran B2C ───
        [HttpGet]
        public async Task<IActionResult> ReturnB2C(string orderId, bool mock = false)
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.Subscription)
                .FirstOrDefaultAsync(p => p.MerchantOrderId == orderId);

            if (payment == null) return NotFound();

            if ((mock || _env.IsDevelopment()) && payment.PaymentStatus == "pending")
                await PaymentCompletionService.MarkPaidAsync(_context, payment);

            if (payment.PaymentStatus == "paid")
            {
                TempData["success"] = "Pembayaran berhasil! Anda sekarang bisa menjadwalkan sesi.";
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = "Pembayaran belum terkonfirmasi. Coba beberapa saat lagi atau hubungi support.";
            return RedirectToAction(nameof(Index));
        }
    }
}
