using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchedulesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs.FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        private static string DisplayStatus(Schedule s)
        {
            if (s.Status == "Cancelled") return "Dibatalkan";
            if (s.Status == "Completed") return "Selesai";
            var now = DateTime.Now;
            var end = s.SessionStart.AddMinutes(s.DurationMinutes);
            if (now < s.SessionStart) return "Akan Datang";
            if (now <= end) return "On Going";
            return "Selesai (otomatis)";
        }

        // ═════════════════════════════════════════════
        //  List
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? period, int page = 1)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var q = _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Psychologist).ThenInclude(p => p!.User)
                .Where(s => s.Patient!.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(s => s.Patient!.User!.FullName.Contains(search));

            if (!string.IsNullOrEmpty(period))
            {
                var today = DateTime.Today;
                DateTime cutoff = period switch
                {
                    "HariIni" => today,
                    "Mingguan" => today.AddDays(-7),
                    "Bulanan" => today.AddDays(-30),
                    _ => DateTime.MinValue
                };
                if (cutoff != DateTime.MinValue) q = q.Where(s => s.SessionStart >= cutoff);
            }

            int pageSize = 10;
            int total = await q.CountAsync();
            var rows = await q
                .OrderByDescending(s => s.SessionStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rows.Select(s => new HrScheduleListItem
            {
                ScheduleId = s.ScheduleId,
                PatientId = s.PatientId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                DbStatus = s.Status,
                DisplayStatus = DisplayStatus(s),
                Notes = s.Notes,
                MeetingLink = s.MeetingLink,
                PsychologistName = s.Psychologist?.User?.FullName ?? "—"
            }).ToList();

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active")
                .OrderBy(p => p.User!.FullName)
                .Select(p => new HrSimplePatient { PatientId = p.PatientId, FullName = p.User!.FullName, Department = p.Department })
                .ToListAsync();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrScheduleListViewModel
            {
                Search = search,
                Period = period,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items,
                AvailablePatients = patients
            });
        }

        // ═════════════════════════════════════════════
        //  Edit (cancel / reschedule)
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var s = await _context.Schedules
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .Include(x => x.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.Patient!.CompanyId == hr.CompanyId);
            if (s == null) return NotFound();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrScheduleEditViewModel
            {
                ScheduleId = s.ScheduleId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                PsychologistName = s.Psychologist?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status,
                Notes = s.Notes
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(HrScheduleEditViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Monitoring";
                return View(model);
            }

            var s = await _context.Schedules
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.ScheduleId == model.ScheduleId && x.Patient!.CompanyId == hr.CompanyId);
            if (s == null) return NotFound();

            s.SessionStart = model.SessionStart;
            s.DurationMinutes = model.DurationMinutes;
            s.Status = model.Status;
            s.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes;
            await _context.SaveChangesAsync();
            TempData["success"] = "Jadwal diperbarui.";
            return RedirectToAction(nameof(Index));
        }

        // ═════════════════════════════════════════════
        //  Request — propose a new session
        // ═════════════════════════════════════════════
        [HttpGet]
        public new async Task<IActionResult> Request()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == hr.CompanyId && p.EmploymentStatus == "active")
                .OrderBy(p => p.User!.FullName)
                .Select(p => new HrSimplePatient
                {
                    PatientId = p.PatientId,
                    FullName = p.User!.FullName,
                    Department = p.Department
                })
                .ToListAsync();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrRequestViewModel
            {
                RequestType = "Schedule",
                AvailablePatients = patients
            });
        }

        [HttpPost]
        public async Task<IActionResult> Request(HrRequestViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Pastikan Anda telah memilih pasien.";
                return RedirectToAction(nameof(Index));
            }

            var psychologistId = await _context.Assignments
                .Where(a => a.PatientId == model.PatientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => (int?)a.PsychologistId)
                .FirstOrDefaultAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                RequestedByHrUserId = user!.Id,
                PatientId = model.PatientId,
                PsychologistId = psychologistId,
                RequestType = "Schedule",
                Notes = model.Notes,
                ProposedSessionDate = model.ProposedSessionDate,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan jadwal dikirim ke psikolog terkait.";
            return RedirectToAction(nameof(Index));
        }

        // ═════════════════════════════════════════════
        //  RequestModal  (AJAX — used by Employee Detail modal)
        // ═════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestModal(
            [FromForm] int patientId,
            [FromForm] string? proposedSessionDate,
            [FromForm] string? notes)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null)
                return Json(new { ok = false, errors = new[] { "Sesi tidak valid. Silakan login ulang." } });

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientId == patientId && p.CompanyId == hr.CompanyId);
            if (patient == null)
                return Json(new { ok = false, errors = new[] { "Karyawan tidak ditemukan." } });

            DateTime? dt = null;
            if (!string.IsNullOrEmpty(proposedSessionDate) &&
                DateTime.TryParse(proposedSessionDate, out var parsed))
                dt = parsed;

            var psychologistId = await _context.Assignments
                .Where(a => a.PatientId == patientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => (int?)a.PsychologistId)
                .FirstOrDefaultAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                RequestedByHrUserId = user!.Id,
                PatientId = patientId,
                PsychologistId = psychologistId,
                RequestType = "Schedule",
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                ProposedSessionDate = dt,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { ok = true, message = "Permintaan jadwal sesi dikirim ke psikolog." });
        }
        [HttpGet]
        public async Task<IActionResult> ScheduleDetailModal(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Unauthorized();

            var schedule = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(s => s.ScheduleId == id && s.Patient!.CompanyId == hr.CompanyId);

            if (schedule == null) return NotFound();

            return PartialView("_ScheduleDetailModal", schedule);
        }
    }
}
