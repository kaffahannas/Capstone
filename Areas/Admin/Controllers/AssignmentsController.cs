using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    // #Class AssignmentsController#
    [Authorize(Roles = "Admin")]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AssignmentActivationService _activation;
        private readonly PsychologistWorkloadService _workload;
        private readonly SubscriptionPricingService _pricing;

        public AssignmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AssignmentActivationService activation,
            PsychologistWorkloadService workload,
            SubscriptionPricingService pricing)
        {
            _context = context;
            _userManager = userManager;
            _activation = activation;
            _workload = workload;
            _pricing = pricing;
        }

        // #Bagian Daftar Penugasan#
        // #Function Index#

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "patient")
        {
            var pendingPatientCount = await _context.PatientAdminAssignmentRequests.CountAsync(r => r.Status == "Pending");
            var pendingB2BCount = await _context.CompanyPsychologistRequests.CountAsync(r => r.PsychologistId == null && r.Status == "Pending");

            var patientRequests = new List<PatientAdminAssignmentRequest>();
            var b2bRequests = new List<CompanyPsychologistRequest>();
            List<PsychologistWorkloadInfo> psychologists = new();
            
            if (tab == "patient")
            {
                patientRequests = await _context.PatientAdminAssignmentRequests
                    .Include(r => r.Patient).ThenInclude(p => p!.User)
                    .Include(r => r.Patient).ThenInclude(p => p!.Company)
                    .Include(r => r.PreferredPsychologist).ThenInclude(p => p!.User)
                    .Where(r => r.Status == "Pending")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                psychologists = await _workload.GetApprovedPsychologistsWithWorkloadAsync(b2bOnly: false);
            }
            else if (tab == "workload")
            {
                psychologists = await _workload.GetApprovedPsychologistsWithWorkloadAsync(b2bOnly: false);
            }
            else if (tab == "b2b")
            {
                b2bRequests = await _context.CompanyPsychologistRequests
                    .Include(r => r.Company)
                    .Where(r => r.PsychologistId == null && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                psychologists = await _workload.GetApprovedPsychologistsWithWorkloadAsync(b2bOnly: true);
            }

            ViewBag.ActiveNav = "Assignments";
            ViewData["Title"] = "Penugasan Psikolog";
            return View(new AdminAssignmentsIndexViewModel
            {
                Tab = tab,
                PatientAdminRequests = patientRequests,
                B2BRequests = b2bRequests,
                Psychologists = psychologists,
                PendingPatientRequestCount = pendingPatientCount,
                PendingB2BRequestCount = pendingB2BCount
            });
        }

        // #Bagian Penugasan Pasien#
        // #Function AssignPatient#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPatient(int requestId, int psychologistId, decimal? psychologistRevenuePercentage, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.PatientAdminAssignmentRequests
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending");
            if (req == null) return NotFound();

            if (await _activation.PatientHasBlockingAssignmentAsync(req.PatientId))
            {
                TempData["error"] = "Pasien sudah memiliki penugasan aktif atau permintaan lain yang sedang diproses.";
                return RedirectToAction(nameof(Index), new { tab = "patient" });
            }

            var psy = await _context.Psychologists.FindAsync(psychologistId);
            if (psy == null)
            {
                TempData["error"] = "Psikolog tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "patient" });
            }

            var assignment = new PatientPsychologistAssignment
            {
                PatientId = req.PatientId,
                PsychologistId = psychologistId,
                AssignedAt = DateTime.UtcNow,
                RequestedByUserId = req.Patient!.UserId,
                RequestedByRole = "Patient"
            };

            await _activation.ActivateAsync(assignment, user?.Id, note, psychologistRevenuePercentage);
            _context.Assignments.Add(assignment);

            req.Status = "Assigned";
            req.AssignedPsychologistId = psychologistId;
            req.AssignedByAdminUserId = user?.Id;
            req.DecisionAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = "Pasien berhasil ditugaskan ke psikolog.";
            return RedirectToAction(nameof(Index), new { tab = "patient" });
        }

        // #Function DismissPatientRequest#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissPatientRequest(int requestId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.PatientAdminAssignmentRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending");
            if (req == null) return NotFound();

            req.Status = "Dismissed";
            req.AssignedByAdminUserId = user?.Id;
            req.DecisionAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(note))
                req.Reason = (req.Reason ?? "") + (string.IsNullOrEmpty(req.Reason) ? "" : " | ") + note;

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pasien ditutup.";
            return RedirectToAction(nameof(Index), new { tab = "patient" });
        }

        // #Bagian Penugasan B2B#
        // #Function AssignB2BPsychologist#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignB2BPsychologist(int requestId, int psychologistId)
        {
            var req = await _context.CompanyPsychologistRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == null && r.Status == "Pending");
            if (req == null) return NotFound();

            req.PsychologistId = psychologistId;
            req.RespondedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan kemitraan telah diteruskan ke psikolog terpilih.";
            return RedirectToAction(nameof(Index), new { tab = "b2b" });
        }

        // #Function DismissB2BRequest#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissB2BRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.CompanyPsychologistRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == null && r.Status == "Pending");
            if (req == null) return NotFound();

            req.Status = "Rejected"; // Or "Dismissed" if we add that status for B2B requests. The model comment says Pending, Approved, Rejected.
            req.RespondedDate = DateTime.UtcNow;
            req.HandledByAdminUserId = user?.Id;
            
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan B2B ditolak / ditutup.";
            return RedirectToAction(nameof(Index), new { tab = "b2b" });
        }
    }
}
