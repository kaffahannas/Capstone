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

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "psy")
        {
            var pendingPsyCount = await _context.Assignments.CountAsync(a => a.Status == "PendingAdminApproval");
            var pendingCancelCount = await _context.Assignments.CountAsync(a => a.Status == "PendingCancellationByAdmin");
            var pendingPatientCount = await _context.PatientAdminAssignmentRequests.CountAsync(r => r.Status == "Pending");

            var pendingAssignments = new List<PatientPsychologistAssignment>();
            if (tab is "psy" or "cancel")
            {
                var status = tab == "psy" ? "PendingAdminApproval" : "PendingCancellationByAdmin";
                pendingAssignments = await _context.Assignments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .Include(a => a.Patient).ThenInclude(p => p!.Company)
                    .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                    .Include(a => a.RequestedBy)
                    .Include(a => a.CancellationRequestedBy)
                    .Where(a => a.Status == status)
                    .OrderByDescending(a => a.AssignedAt)
                    .ToListAsync();
            }

            var patientRequests = new List<PatientAdminAssignmentRequest>();
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

            ViewBag.ActiveNav = "Assignments";
            ViewData["Title"] = "Permintaan Penugasan";
            return View(new AdminAssignmentsIndexViewModel
            {
                Tab = tab,
                PendingAssignments = pendingAssignments,
                PatientAdminRequests = patientRequests,
                Psychologists = psychologists,
                PendingPsyCount = pendingPsyCount,
                PendingCancelCount = pendingCancelCount,
                PendingPatientRequestCount = pendingPatientCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int assignmentId, string? note, decimal? psychologistRevenuePercentage)
        {
            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments.FindAsync(assignmentId);
            if (a == null) return NotFound();

            var returnTab = a.Status == "PendingCancellationByAdmin" ? "cancel" : "psy";

            if (a.Status == "PendingAdminApproval")
            {
                await _activation.ActivateAsync(a, user?.Id, note, psychologistRevenuePercentage);
            }
            else if (a.Status == "PendingCancellationByAdmin")
            {
                a.Status = "Cancelled";
                a.DecisionByUserId = user?.Id;
                a.DecisionAt = DateTime.UtcNow;
                a.DecisionNote = note;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan disetujui.";
            return RedirectToAction(nameof(Index), new { tab = returnTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int assignmentId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments.FindAsync(assignmentId);
            if (a == null) return NotFound();

            var returnTab = a.Status == "PendingCancellationByAdmin" ? "cancel" : "psy";

            if (a.Status == "PendingCancellationByAdmin")
                a.Status = "Active";
            else
                a.Status = "Rejected";

            a.DecisionByUserId = user?.Id;
            a.DecisionAt = DateTime.UtcNow;
            a.DecisionNote = note;

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan ditolak.";
            return RedirectToAction(nameof(Index), new { tab = returnTab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPatient(int requestId, int psychologistId, decimal psychologistRevenuePercentage, string? note)
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

            if (psychologistRevenuePercentage is < 0 or > 100)
                psychologistRevenuePercentage = SubscriptionPricingService.DefaultPsychologistRevenuePercentage;

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
    }
}
