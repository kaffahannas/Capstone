using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.Constants;
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
    // #Class PsychologistsController#
    [RequiresPatientPremium]
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

        // ─── Daftar psikolog yang bisa dipilih pasien ───
        // #Function Index#
        // #Bagian Daftar Psikolog#
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return RedirectToAction("Welcome", "Onboarding", new { area = "Patient" });

            // Already has an active psychologist?
            var activeAssignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId &&
                    (a.Status == "Active" || a.Status == "PendingPsychologistApproval" || a.Status == "PendingAdminApproval"));

            var pendingAdminRequest = await _context.PatientAdminAssignmentRequests
                .FirstOrDefaultAsync(r => r.PatientId == patient.PatientId && r.Status == "Pending");

            var hasActivePsychologist = await _context.Assignments
                .AnyAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");

            // All available psychologists (approved, accepting B2B if employee; any if B2C)
            IQueryable<PsychologistModel> query = _context.Psychologists
                .Include(p => p.User)
                .Where(p => p.User != null && p.User.IsApprovedByAdmin);

            if (patient.CompanyId != null)
                query = query.Where(p => p.AcceptsB2B);

            var psychologists = await query.OrderBy(p => p.User!.FullName).ToListAsync();

            ViewBag.ActiveAssignment = activeAssignment;
            ViewBag.PendingAdminRequest = pendingAdminRequest;
            ViewBag.HasActivePsychologist = hasActivePsychologist;
            ViewBag.PatientId = patient.PatientId;
            ViewBag.ActiveNav = "Psikolog";
            ViewData["Title"] = "Pilih Psikolog";
            return View(psychologists);
        }

        // ─── Pasien memilih psikolog (Request → PendingPsychologistApproval) ───
        // #Function Request#
        // #Bagian Pilih Psikolog#
        [HttpPost]
        public new async Task<IActionResult> Request(int psychologistId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            var psy = await _context.Psychologists.FindAsync(psychologistId);
            if (psy == null) return NotFound();

            // Prevent duplicate / handle undo of pending cancellation to same psychologist
            var pendingCancelSamePsych = await _context.Assignments.FirstOrDefaultAsync(a =>
                a.PatientId == patient.PatientId &&
                a.PsychologistId == psychologistId &&
                (a.Status == AssignmentStatus.PendingCancellationByHr ||
                 a.Status == AssignmentStatus.PendingCancellationByAdmin));
            if (pendingCancelSamePsych != null)
            {
                pendingCancelSamePsych.Status = AssignmentStatus.Active;
                pendingCancelSamePsych.CancellationRequestedByUserId = null;
                pendingCancelSamePsych.CancellationReason = null;
                pendingCancelSamePsych.CancellationRequestedAt = null;
                await _context.SaveChangesAsync();
                TempData["success"] = "Permintaan pembatalan dibatalkan. Psikolog Anda tetap aktif.";
                return RedirectToAction(nameof(Index));
            }

            if (await _activation.PatientHasBlockingAssignmentAsync(patient.PatientId))
            {
                TempData["error"] = "Anda sudah memiliki psikolog aktif atau permintaan yang sedang diproses.";
                return RedirectToAction(nameof(Index));
            }

            if (await _activation.HasLiveAssignmentForPairAsync(patient.PatientId, psychologistId))
            {
                TempData["error"] = "Permintaan untuk psikolog ini sudah ada atau sedang diproses.";
                return RedirectToAction(nameof(Index));
            }

            var assignment = new PatientPsychologistAssignment
            {
                PatientId = patient.PatientId,
                PsychologistId = psychologistId,
                Status = AssignmentStatus.PendingPsychologistApproval,
                AssignedAt = DateTime.UtcNow,
                RequestedByUserId = user.Id,
                RequestedByRole = "Patient"
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan pilihan psikolog berhasil dikirim. Menunggu persetujuan psikolog.";
            return RedirectToAction(nameof(Index));
        }

        // #Function RequestAdminAssignment#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAdminAssignment(int? preferredPsychologistId, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            var hasActive = await _context.Assignments
                .AnyAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (hasActive)
            {
                TempData["error"] = "Anda sudah memiliki psikolog aktif.";
                return RedirectToAction(nameof(Index));
            }

            if (await _activation.PatientHasBlockingAssignmentAsync(patient.PatientId))
            {
                TempData["error"] = "Masih ada permintaan penugasan yang sedang diproses.";
                return RedirectToAction(nameof(Index));
            }

            var existingRequest = await _context.PatientAdminAssignmentRequests
                .AnyAsync(r => r.PatientId == patient.PatientId && r.Status == "Pending");
            if (existingRequest)
            {
                TempData["error"] = "Permintaan ke Admin sudah dikirim sebelumnya.";
                return RedirectToAction(nameof(Index));
            }

            _context.PatientAdminAssignmentRequests.Add(new PatientAdminAssignmentRequest
            {
                PatientId = patient.PatientId,
                PreferredPsychologistId = preferredPsychologistId,
                Reason = reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan penugasan ke Admin berhasil dikirim. Tim LightenUp akan menugaskan psikolog untuk Anda.";
            return RedirectToAction(nameof(Index));
        }
    }
}
