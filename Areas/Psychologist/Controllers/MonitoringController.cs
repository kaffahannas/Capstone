using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    /// <summary>Dashboard monitoring klien klinik psikolog Mitra — mirip pola Hr/EmployeesController.</summary>
    [Area("Psychologist")]
    [Authorize(Roles = "Psychologist")]
    [RequiresPsychologistMitra]
    public class MonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MonitoringController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<List<PsyPatientOption>> LoadMitraPatientOptionsAsync(int psyId)
        {
            return await _context.Patients
                .Where(p => p.SponsorPsychologistId == psyId && p.SponsorType == "Psychologist")
                .Include(p => p.User)
                .Select(p => new PsyPatientOption { PatientId = p.PatientId, FullName = p.User!.FullName, CompanyName = "Klien Klinik" })
                .OrderBy(o => o.FullName)
                .ToListAsync();
        }

        private async Task<LightenUp.Web.Models.Psychologist?> GetPsyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ─── Daftar klien klinik + status mental ───
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var clients = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Assignments)
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId)
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            ViewBag.ActiveNav = "MonitoringKlien";
            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Monitoring Klien Klinik";
            return View(clients);
        }

        // ─── Detail klien klinik ───
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == id && p.SponsorPsychologistId == psy.PsychologistId);
            if (patient == null) return NotFound();

            // Mood chart (7 hari)
            var from7 = DateTime.Today.AddDays(-6);
            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == id && m.MoodDate >= from7)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();
            var chartDates = Enumerable.Range(0, 7).Select(i => from7.AddDays(i)).ToList();
            var chartScores = chartDates.Select(d => {
                var m = moods.FirstOrDefault(x => x.MoodDate.Date == d.Date);
                if (m == null) return (double?)null;
                return (double?)(m.Feeling switch {
                    "Overjoyed" => 5, "Happy" => 4, "Calm" => 4,
                    "Neutral" => 3, "Disappointed" => 2, "Angry" => 1, _ => (int?)null
                });
            }).ToList();

            int sehatN = 0, beresikoN = 0, bahayaN = 0;
            foreach (var s in chartScores.Where(x => x.HasValue).Select(x => x!.Value))
            {
                if (s >= 4) sehatN++;
                else if (s >= 2.5) beresikoN++;
                else bahayaN++;
            }
            int totalN = Math.Max(1, sehatN + beresikoN + bahayaN);

            var todaySession = await _context.Schedules
                .Where(s => s.PatientId == id && s.SessionStart >= DateTime.Today && s.SessionStart < DateTime.Today.AddDays(1) && s.Status == "Scheduled")
                .OrderBy(s => s.SessionStart)
                .FirstOrDefaultAsync();
            var openWorksheetCount = await _context.Worksheets.CountAsync(w => w.PatientId == id && w.Status != "Completed");

            string ageStr = "Belum diatur";
            if (patient.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                var birth = patient.DateOfBirth.Value;
                int age = today.Year - birth.Year;
                if (birth.Date > today.AddYears(-age)) age--;
                ageStr = $"{age} tahun";
            }

            ViewBag.MoodLabels = System.Text.Json.JsonSerializer.Serialize(chartDates.Select(d => d.ToString("dd/MM")));
            ViewBag.MoodScores = System.Text.Json.JsonSerializer.Serialize(chartScores);
            ViewBag.HasMoodData = moods.Any();
            ViewBag.SehatPct = (int)Math.Round((double)sehatN / totalN * 100);
            ViewBag.BeresikoPct = (int)Math.Round((double)beresikoN / totalN * 100);
            ViewBag.BahayaPct = (int)Math.Round((double)bahayaN / totalN * 100);
            ViewBag.TodaySession = todaySession;
            ViewBag.OpenWorksheetCount = openWorksheetCount;
            ViewBag.AgeStr = ageStr;
            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = $"Detail — {patient.User?.FullName}";
            return View(patient);
        }

        // ─── Statistik agregat klien klinik ───
        [HttpGet]
        public async Task<IActionResult> Statistik()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var clients = await _context.Patients
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId)
                .ToListAsync();

            ViewBag.TotalClients = clients.Count;
            ViewBag.SehatCount = clients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = clients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = clients.Count(p => p.MentalHealthStatus == "Bahaya");
            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Statistik Klien Klinik";
            return View();
        }

        // ─── Progress worksheet klien klinik ───
        [HttpGet]
        public async Task<IActionResult> Worksheet()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var clientIds = await _context.Patients
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId)
                .Select(p => p.PatientId)
                .ToListAsync();

            var worksheets = await _context.Worksheets
                .Include(w => w.Patient).ThenInclude(p => p!.User)
                .Where(w => clientIds.Contains(w.PatientId))
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            ViewBag.CountBelum   = worksheets.Count(w => w.Status == "Assigned");
            ViewBag.CountReview  = worksheets.Count(w => w.Status == "InReview");
            ViewBag.CountSelesai = worksheets.Count(w => w.Status == "Completed");
            ViewBag.AddTaskForm = new PsyAddTaskViewModel
            {
                AvailablePatients = await LoadMitraPatientOptionsAsync(psy.PsychologistId),
                MitraOnly = true
            };
            ViewBag.OpenAddTaskModal = false;
            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Worksheet Klien Klinik";
            return View(worksheets);
        }

        // ─── Jadwal sesi klien klinik ───
        [HttpGet]
        public async Task<IActionResult> Jadwal(string? filter)
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var clientIds = await _context.Patients
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId)
                .Select(p => p.PatientId)
                .ToListAsync();

            var allSchedules = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Where(s => clientIds.Contains(s.PatientId))
                .OrderBy(s => s.SessionStart)
                .ToListAsync();

            var now = DateTime.Now;
            ViewBag.Today   = DateTime.Today;
            ViewBag.Filter  = filter ?? "Semua";
            ViewBag.AllSessionsInWindow = allSchedules;
            ViewBag.CountTotal     = allSchedules.Count;
            ViewBag.CountUpcoming  = allSchedules.Count(s => s.Status != "Completed" && s.Status != "Cancelled" && s.SessionStart >= now);
            ViewBag.CountCompleted = allSchedules.Count(s => s.Status == "Completed" || (s.Status == "Scheduled" && s.SessionStart.AddMinutes(s.DurationMinutes) <= now));
            ViewBag.CountCancelled = allSchedules.Count(s => s.Status == "Cancelled");

            var schedules = filter switch {
                "Selesai"    => allSchedules.Where(s => s.Status == "Completed" || (s.Status == "Scheduled" && s.SessionStart.AddMinutes(s.DurationMinutes) <= now)).ToList(),
                "Dibatalkan" => allSchedules.Where(s => s.Status == "Cancelled").ToList(),
                _            => allSchedules
            };

            ViewBag.AddScheduleForm = new PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadMitraPatientOptionsAsync(psy.PsychologistId),
                MitraOnly = true
            };
            ViewBag.OpenAddScheduleModal = false;
            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Jadwal Klien Klinik";
            return View(schedules);
        }
    }
}
