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

        public PsychologistsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AssignmentActivationService activation)
        {
            _context = context;
            _userManager = userManager;
            _activation = activation;
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

            var hasActivePsychologist = await _context.Assignments
                .AnyAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");

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

            // Cegah duplikat assignment aktif
            var existing = await _context.Assignments.AnyAsync(a =>
                a.PatientId == patient.PatientId &&
                (a.Status == "Active" || a.Status == "PendingCancellation" || a.Status == "PendingCancellationByHr"));
            if (existing)
            {
                TempData["error"] = "Anda sudah memiliki psikolog aktif.";
                return RedirectToAction(nameof(Index));
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
            }
            else
            {
                // B2C Publik: Buat Subscription
                // Note: Implementasi payment gateway (Duitku) idealnya dipanggil di sini.
                // Untuk simulasi, kita langsung buat Subscription aktif.
                var subscription = new Subscription
                {
                    PatientId = patient.PatientId,
                    PsychologistId = psychologistId,
                    PlanName = "1-on-1 Counseling",
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    MaxSessionsPerMonth = psy.SessionTokensPerMonth > 0 ? psy.SessionTokensPerMonth : 4
                };
                _context.Subscriptions.Add(subscription);
            }

            // Langsung Active — tidak perlu approval
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
    }
}
