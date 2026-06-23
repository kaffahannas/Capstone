using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
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

            var recentMoods = await _context.MoodTrackers
                .Where(m => m.PatientId == id)
                .OrderByDescending(m => m.MoodDate)
                .Take(7)
                .ToListAsync();

            var worksheets = await _context.Worksheets
                .Where(w => w.PatientId == id)
                .OrderByDescending(w => w.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentMoods = recentMoods;
            ViewBag.Worksheets = worksheets;
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

            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Worksheet Klien Klinik";
            return View(worksheets);
        }

        // ─── Jadwal sesi klien klinik ───
        [HttpGet]
        public async Task<IActionResult> Jadwal()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var clientIds = await _context.Patients
                .Where(p => p.SponsorPsychologistId == psy.PsychologistId)
                .Select(p => p.PatientId)
                .ToListAsync();

            var schedules = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Where(s => clientIds.Contains(s.PatientId) && s.SessionStart >= DateTime.Today)
                .OrderBy(s => s.SessionStart)
                .ToListAsync();

            ViewBag.ActiveTab = "Mitra";
            ViewData["Title"] = "Jadwal Klien Klinik";
            return View(schedules);
        }
    }
}
