using LightenUp.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace LightenUp.Web.Areas.Patient.Controllers
{
// #Class JadwalController#
[Area("Patient")]
[Authorize(Roles = "Patient")]
[RequiresPatientPremium]
public class JadwalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JadwalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // #Function Index#

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveNav = "Jadwal";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Schedules)
                    .ThenInclude(s => s.Psychologist)
                        .ThenInclude(psy => psy.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null) return NotFound("Patient record not found.");

            var vm = new JadwalViewModel();

            var allSchedules = patient.Schedules
                .Select(s => new JadwalItemViewModel
                {
                    ScheduleId = s.ScheduleId,
                    PsychologistName = s.Psychologist?.User?.FullName ?? "Dr. Unknown",
                    SessionStart = s.SessionStart,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status,
                    MeetingLink = s.MeetingLink
                })
                .OrderBy(s => s.SessionStart)
                .ToList();

            var today = DateTime.UtcNow;

            vm.UpcomingSessions = allSchedules.Where(s => s.SessionStart >= today || s.Status == "Scheduled").ToList();
            vm.PastSessions = allSchedules.Where(s => s.SessionStart < today && s.Status != "Scheduled").OrderByDescending(s => s.SessionStart).ToList();

            // Populate active psychologist info for the request session modal
            var activeAssignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");

            vm.HasActivePsychologist = activeAssignment != null;
            vm.PsychologistName = activeAssignment?.Psychologist?.User?.FullName;
            vm.PsychologistId = activeAssignment?.PsychologistId;

            // Pass to ViewBag for the modal partial
            ViewBag.PsychologistName = vm.PsychologistName;
            ViewBag.PsychologistId = vm.PsychologistId;
            ViewBag.PatientId = patient.PatientId;

            return View(vm);
        }

        // ─── Pasien meminta jadwal sesi konseling (gated: premium) ───
        // #Function RequestSession#
        // #Bagian Permintaan Jadwal#
        [HttpGet]
        public async Task<IActionResult> RequestSession()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            // Must have an active psychologist
            var assignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (assignment == null)
            {
                TempData["error"] = "Anda belum memiliki psikolog aktif. Pilih psikolog terlebih dahulu.";
                return RedirectToAction("Index", "Psychologists");
            }

            ViewBag.PsychologistName = assignment.Psychologist?.User?.FullName ?? "—";
            ViewBag.PsychologistId = assignment.PsychologistId;
            ViewBag.PatientId = patient.PatientId;
            ViewBag.ActiveNav = "Jadwal";
            return View();
        }

        // #Function RequestSession POST#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSession(DateTime proposedDate, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (assignment == null)
            {
                TempData["error"] = "Anda belum memiliki psikolog aktif.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent spamming
            var recentPending = await _context.PsychologistRequests.AnyAsync(r =>
                r.PatientId == patient.PatientId &&
                r.PsychologistId == assignment.PsychologistId &&
                r.RequestType == "Schedule" &&
                r.RequesterRole == "Patient" &&
                r.Status == "Pending");
            if (recentPending)
            {
                TempData["info"] = "Anda sudah memiliki permintaan jadwal yang sedang menunggu.";
                return RedirectToAction(nameof(Index));
            }

            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                PatientId = patient.PatientId,
                PsychologistId = assignment.PsychologistId,
                RequestedByPatientUserId = user.Id,
                RequesterRole = "Patient",
                RequestType = "Schedule",
                ProposedSessionDate = proposedDate,
                Notes = notes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan jadwal sesi berhasil dikirim ke psikolog Anda.";
            return RedirectToAction(nameof(Index));
        }
    }
}
