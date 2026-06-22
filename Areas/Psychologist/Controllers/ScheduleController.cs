using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    // #Class ScheduleController#
    [Authorize(Roles = "Psychologist")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ScheduleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<int?> CurrentPsychologistIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.PsychologistId).FirstOrDefaultAsync();
        }

        private async Task<List<LightenUp.Web.Models.ViewModels.PsyPatientOption>> LoadPatientOptionsAsync(int psyId)
        {
            return await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellation"))
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Select(a => new LightenUp.Web.Models.ViewModels.PsyPatientOption
                {
                    PatientId = a.PatientId,
                    FullName = a.Patient!.User!.FullName,
                    CompanyName = a.Patient.Company != null ? a.Patient.Company.Name : null
                })
                .Distinct()
                .OrderBy(o => o.FullName)
                .ToListAsync();
        }

        // #Function AddSchedule#

        [HttpGet]
        public async Task<IActionResult> AddSchedule(int? patientId = null)
        {
            if (patientId.HasValue)
                return RedirectToAction(nameof(PatientScheduleHistory), new { id = patientId.Value, add = true });
            return RedirectToAction(nameof(Scheduling), new { add = true, patientId });
        }

        // #Function AddSchedule POST#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            if (model.PatientId <= 0)
                ModelState.AddModelError(nameof(model.PatientId), "Pilih pasien.");

            if (!ModelState.IsValid)
            {
                model.AvailablePatients = await LoadPatientOptionsAsync(psyId.Value);
                if (model.ReturnPatientId.HasValue)
                    return await PatientScheduleHistoryViewAsync(model.ReturnPatientId.Value, model, openModal: true);
                return await SchedulingViewAsync(model.ReturnFilter ?? "Semua", model, openModal: true);
            }

            var sessionStart = model.SessionDate.Date.Add(model.SessionTime);
            
            decimal? appliedPercentage = null;
            decimal? slotValue = null;
            var patient = await _context.Patients
                .Include(p => p.Company)
                .ThenInclude(c => c.Subscriptions)
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.PatientId == model.PatientId);

            var psySettings = await _context.PayrollSettings
                .FirstOrDefaultAsync(ps => ps.PsychologistId == psyId.Value);
            
            if (patient != null)
            {
                var pricing = new SubscriptionPricingService(_context);
                var pricingResult = await pricing.GetSlotValueForPatientAsync(model.PatientId);
                slotValue = pricingResult.SlotValue;
                int maxSessions = pricingResult.MaxSessions;

                var firstDayOfMonth = new DateTime(sessionStart.Year, sessionStart.Month, 1);
                var nextMonth = firstDayOfMonth.AddMonths(1);

                var monthlySessionsCount = await _context.Schedules
                    .CountAsync(s => s.PatientId == model.PatientId && 
                                     s.SessionStart >= firstDayOfMonth && 
                                     s.SessionStart < nextMonth &&
                                     s.Status != "Cancelled");

                if (monthlySessionsCount >= maxSessions)
                {
                    ModelState.AddModelError(string.Empty, $"Klien telah mencapai batas maksimal {maxSessions} sesi per bulan berdasarkan kontrak perusahaan atau langganan B2C.");
                    model.AvailablePatients = await LoadPatientOptionsAsync(psyId.Value);
                    if (model.ReturnPatientId.HasValue)
                        return await PatientScheduleHistoryViewAsync(model.ReturnPatientId.Value, model, openModal: true);
                    return await SchedulingViewAsync(model.ReturnFilter ?? "Semua", model, openModal: true);
                }

                if (psySettings != null && psySettings.Status == "Active")
                {
                    appliedPercentage = psySettings.PsychologistPercentage;
                }
            }
            
            _context.Schedules.Add(new Schedule
            {
                PsychologistId = psyId.Value,
                PatientId = model.PatientId,
                SessionStart = sessionStart,
                DurationMinutes = model.DurationMinutes,
                Status = "Scheduled",
                Notes = model.Notes,
                MeetingLink = model.MeetingLink,
                AppliedPercentage = appliedPercentage,
                SlotValue = slotValue
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Jadwal konseling baru ditambahkan.";

            if (model.ReturnPatientId.HasValue)
                return RedirectToAction(nameof(PatientScheduleHistory), new { id = model.ReturnPatientId.Value });
            return RedirectToAction(nameof(Scheduling), new { filter = model.ReturnFilter ?? "Semua" });
        }

        // #Function EditSchedule#

        [HttpGet]
        public async Task<IActionResult> EditSchedule(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var s = await _context.Schedules
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            var model = new LightenUp.Web.Models.ViewModels.PsyScheduleEditViewModel
            {
                ScheduleId = s.ScheduleId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status,
                Notes = s.Notes,
                MeetingLink = s.MeetingLink,
                ExistingProofPath = s.ProofOfCompletionPath
            };

            return View(model);
        }

        // #Function EditSchedule POST#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(LightenUp.Web.Models.ViewModels.PsyScheduleEditViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            if (!ModelState.IsValid)
                return View(model);

            var s = await _context.Schedules
                .FirstOrDefaultAsync(x => x.ScheduleId == model.ScheduleId && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            if (model.Status == "Completed")
            {
                if (string.IsNullOrWhiteSpace(model.MeetingLink))
                {
                    ModelState.AddModelError("MeetingLink", "Link Google Meet wajib diisi untuk menandai sesi sebagai selesai.");
                    return View(model);
                }

                if (model.ProofFile == null && string.IsNullOrWhiteSpace(s.ProofOfCompletionPath))
                {
                    ModelState.AddModelError("ProofFile", "Bukti penyelesaian wajib diunggah untuk menandai sesi sebagai selesai.");
                    return View(model);
                }
            }

            if (model.ProofFile != null)
            {
                var uploadsFolder = System.IO.Path.Combine(_env.WebRootPath, "uploads", "proofs");
                System.IO.Directory.CreateDirectory(uploadsFolder);
                var uniqueName = Guid.NewGuid().ToString() + "_" + System.IO.Path.GetFileName(model.ProofFile.FileName);
                var filePath = System.IO.Path.Combine(uploadsFolder, uniqueName);
                using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await model.ProofFile.CopyToAsync(fs);
                }
                s.ProofOfCompletionPath = "/uploads/proofs/" + uniqueName;
            }

            s.SessionStart = model.SessionStart;
            s.DurationMinutes = model.DurationMinutes;
            s.Status = model.Status;
            s.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes;
            s.MeetingLink = string.IsNullOrWhiteSpace(model.MeetingLink) ? null : model.MeetingLink;

            await _context.SaveChangesAsync();
            TempData["success"] = "Jadwal sesi berhasil diperbarui.";
            return RedirectToAction(nameof(Scheduling));
        }

        // #Function CancelSchedule#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSchedule(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var s = await _context.Schedules
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            if (s.Status != "Completed")
            {
                s.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["success"] = "Jadwal berhasil dibatalkan.";
            }

            return RedirectToAction(nameof(Scheduling));
        }

        // #Function MarkNoShow#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var s = await _context.Schedules
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            if (s.Status == "Scheduled" && DateTime.UtcNow > s.SessionStart)
            {
                s.Status = "NoShow";
                await _context.SaveChangesAsync();
                TempData["success"] = "Jadwal ditandai sebagai Mangkir (No-Show).";
            }
            else if (s.Status == "Scheduled")
            {
                TempData["error"] = "Sesi belum lewat waktunya.";
            }

            return RedirectToAction(nameof(Scheduling));
        }

        // #Function Scheduling#

        [HttpGet]
        public async Task<IActionResult> Scheduling(string filter = "Semua", bool add = false, int? patientId = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                ReturnFilter = filter
            };
            if (patientId.HasValue) addForm.PatientId = patientId.Value;

            return await SchedulingViewAsync(filter, addForm, openModal: add);
        }

        private async Task<IActionResult> SchedulingViewAsync(
            string filter,
            LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel? addForm = null,
            bool openModal = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var today = DateTime.Today;
            var monthEnd = today.AddDays(60);

            var q = _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Patient).ThenInclude(p => p!.Company)
                .Where(s => s.PsychologistId == psyId && s.SessionStart >= today.AddDays(-30) && s.SessionStart < monthEnd);

            var allSessionsInWindow = await q.OrderBy(s => s.SessionStart).ToListAsync();

            var now = DateTime.UtcNow;
            var sessions = filter switch
            {
                "Selesai" => allSessionsInWindow.Where(s => s.Status == "Completed" || (s.Status == "Scheduled" && s.SessionStart.AddMinutes(s.DurationMinutes) <= now)).ToList(),
                "Dibatalkan" => allSessionsInWindow.Where(s => s.Status == "Cancelled").ToList(),
                _ => allSessionsInWindow
            };

            ViewBag.AllSessionsInWindow = allSessionsInWindow;
            ViewBag.Today = today;
            ViewBag.Filter = filter;
            ViewBag.Sessions = sessions;
            ViewBag.AddScheduleForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                ReturnFilter = filter
            };
            ViewBag.OpenAddScheduleModal = openModal;
            return View("Scheduling");
        }

        // #Function ScheduleHistory#

        [HttpGet]
        public async Task<IActionResult> ScheduleHistory()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var sessions = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Where(s => s.PsychologistId == psyId)
                .OrderByDescending(s => s.SessionStart)
                .Take(50)
                .ToListAsync();

            ViewBag.Sessions = sessions;
            return View();
        }

        // #Function ScheduleDetailModal#

        [HttpGet]
        public async Task<IActionResult> ScheduleDetailModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var schedule = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Patient).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(s => s.ScheduleId == id && s.PsychologistId == psyId);

            if (schedule == null) return NotFound();

            return PartialView("_ScheduleDetailModal", schedule);
        }

        // #Function EditScheduleModal#

        [HttpGet]
        public async Task<IActionResult> EditScheduleModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var s = await _context.Schedules
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            var model = new LightenUp.Web.Models.ViewModels.PsyScheduleEditViewModel
            {
                ScheduleId = s.ScheduleId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status,
                Notes = s.Notes,
                MeetingLink = s.MeetingLink
            };

            return PartialView("_EditScheduleModal", model);
        }

        // #Function PatientScheduleHistory#

        [HttpGet]
        public async Task<IActionResult> PatientScheduleHistory(int id, bool add = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return NotFound();

            var sessions = await _context.Schedules
                .Where(s => s.PsychologistId == psyId && s.PatientId == id)
                .OrderByDescending(s => s.SessionStart)
                .ToListAsync();

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = id,
                ReturnPatientId = id
            };

            return await PatientScheduleHistoryViewAsync(id, addForm, openModal: add, patientName: patient.User?.FullName ?? "—", sessions: sessions);
        }

        private async Task<IActionResult> PatientScheduleHistoryViewAsync(
            int patientId,
            LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel? addForm = null,
            bool openModal = false,
            string? patientName = null,
            List<Schedule>? sessions = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            if (patientName == null || sessions == null)
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);
                if (patient == null) return NotFound();
                patientName = patient.User?.FullName ?? "—";
                sessions = await _context.Schedules
                    .Where(s => s.PsychologistId == psyId && s.PatientId == patientId)
                    .OrderByDescending(s => s.SessionStart)
                    .ToListAsync();
            }

            ViewBag.PatientName = patientName;
            ViewBag.PatientId = patientId;
            ViewBag.Sessions = sessions;
            ViewBag.AddScheduleForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = patientId,
                ReturnPatientId = patientId
            };
            ViewBag.OpenAddScheduleModal = openModal;
            return View("PatientScheduleHistory");
        }
    }
}
