using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class StatistikController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HealthStatusService _healthService;

        public StatistikController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
                                   HealthStatusService healthService)
        {
            _context = context;
            _userManager = userManager;
            _healthService = healthService;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs.Include(h => h.Company)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        private static double FeelingScore(string feeling) => feeling switch
        {
            "Overjoyed" => 5, "Happy" => 4, "Calm" => 4,
            "Neutral" => 3, "Disappointed" => 2, "Angry" => 1,
            _ => 0
        };

        // ═══════════════════════════════════════
        //  Overview
        // ═══════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var patients = await _context.Patients
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active")
                .ToListAsync();

            var vm = new HrStatistikOverviewViewModel
            {
                CompanyName = hr.Company?.Name ?? "",
                ActiveCount = patients.Count,
                Sehat = patients.Count(p => p.MentalHealthStatus == "Sehat"),
                Beresiko = patients.Count(p => p.MentalHealthStatus == "Beresiko"),
                Bahaya = patients.Count(p => p.MentalHealthStatus == "Bahaya"),
                Divisions = patients
                    .Where(p => !string.IsNullOrEmpty(p.Department))
                    .GroupBy(p => p.Department!)
                    .Select(g =>
                    {
                        var total = g.Count();
                        var s = g.Count(x => x.MentalHealthStatus == "Sehat");
                        var b = g.Count(x => x.MentalHealthStatus == "Beresiko");
                        var d = g.Count(x => x.MentalHealthStatus == "Bahaya");
                        return new DivisionStress
                        {
                            Name = g.Key,
                            Count = total,
                            StressPct = total == 0 ? 0 : (int)Math.Round((double)(b + d) / total * 100),
                            SehatPct = total == 0 ? 0 : (int)Math.Round((double)s / total * 100),
                            BeresikoPct = total == 0 ? 0 : (int)Math.Round((double)b / total * 100),
                            BahayaPct = total == 0 ? 0 : (int)Math.Round((double)d / total * 100),
                            SehatCount = s,
                            BeresikoCount = b,
                            BahayaCount = d
                        };
                    })
                    .OrderBy(d => d.Name)
                    .ToList()
            };

            ViewBag.ActiveNav = "Statistik";
            return View(vm);
        }

        // ═══════════════════════════════════════
        //  Division report
        // ═══════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Division(string name, int window = 30)
        {
            if (window != 7 && window != 30 && window != 90) window = 30;
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var employees = await _context.Patients
                .Where(p => p.CompanyId == companyId && p.Department == name && p.EmploymentStatus == "active")
                .ToListAsync();
            if (employees.Count == 0) return NotFound();

            var ids = employees.Select(p => p.PatientId).ToList();
            var from = DateTime.Today.AddDays(-window + 1);

            var moods = await _context.MoodTrackers
                .Where(m => ids.Contains(m.PatientId) && m.MoodDate >= from)
                .ToListAsync();

            // Build per-day average across employees
            var dates = Enumerable.Range(0, window).Select(i => from.AddDays(i)).ToList();
            var avgScores = dates.Select(d =>
            {
                var dayMoods = moods.Where(m => m.MoodDate.Date == d.Date).ToList();
                if (dayMoods.Count == 0) return 0.0;
                return Math.Round(dayMoods.Average(m => FeelingScore(m.Feeling)), 2);
            }).ToList();

            var vm = new HrDivisionReportViewModel
            {
                DivisionName = name,
                Window = window,
                Dates = dates,
                AvgScores = avgScores,
                Sehat = employees.Count(p => p.MentalHealthStatus == "Sehat"),
                Beresiko = employees.Count(p => p.MentalHealthStatus == "Beresiko"),
                Bahaya = employees.Count(p => p.MentalHealthStatus == "Bahaya")
            };

            ViewBag.ActiveNav = "Statistik";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DivisionChartData(string name, int window = 30)
        {
            if (window != 7 && window != 30 && window != 90) window = 30;
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Unauthorized();
            var companyId = hr.CompanyId.Value;

            var employees = await _context.Patients
                .Where(p => p.CompanyId == companyId && p.Department == name && p.EmploymentStatus == "active")
                .ToListAsync();

            var ids = employees.Select(p => p.PatientId).ToList();
            var from = DateTime.Today.AddDays(-window + 1);

            var moods = await _context.MoodTrackers
                .Where(m => ids.Contains(m.PatientId) && m.MoodDate >= from)
                .ToListAsync();

            var dates = Enumerable.Range(0, window).Select(i => from.AddDays(i)).ToList();
            var avgScores = dates.Select(d =>
            {
                var dayMoods = moods.Where(m => m.MoodDate.Date == d.Date).ToList();
                if (dayMoods.Count == 0) return 0.0;
                return Math.Round(dayMoods.Average(m => FeelingScore(m.Feeling)), 2);
            }).ToList();

            return Json(new { 
                labels = dates.Select(d => d.ToString("dd/MM")), 
                data = avgScores 
            });
        }

        // ═══════════════════════════════════════
        //  Print-optimized view (browser → Save as PDF)
        // ═══════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Print()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var roster = await BuildRosterAsync(companyId);
            ViewBag.Roster = roster;
            ViewBag.Company = hr.Company?.Name ?? "";
            ViewBag.Now = DateTime.Now;
            return View();
        }

        // ═══════════════════════════════════════
        //  CSV export
        // ═══════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var roster = await BuildRosterAsync(companyId);

            var sb = new StringBuilder();
            sb.AppendLine("FullName,Email,Division,EmployeeId,Status,LastCheck,TotalMoodPercent");
            foreach (var r in roster)
            {
                sb.AppendLine($"{Csv(r.FullName)},{Csv(r.Email)},{Csv(r.Division)},{Csv(r.EmployeeId)},{Csv(r.MentalHealthStatus)},{Csv(r.LastCheckDate)},{r.TotalMoodPercent}");
            }

            var fileName = $"lightenup-employees-{DateTime.Now:yyyy-MM-dd}.csv";
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", fileName);
        }

        private async Task<List<HrRosterRow>> BuildRosterAsync(int companyId)
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active")
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            var list = new List<HrRosterRow>();
            foreach (var p in patients)
            {
                var snap = await _healthService.ComputeAsync(p.PatientId);
                list.Add(new HrRosterRow
                {
                    FullName = p.User?.FullName ?? "",
                    Email = p.User?.Email ?? "",
                    Division = p.Department,
                    EmployeeId = p.EmployeeId,
                    MentalHealthStatus = p.MentalHealthStatus,
                    LastCheckDate = snap.LastCheckLabel,
                    TotalMoodPercent = snap.TotalMoodPercent
                });
            }
            return list;
        }

        private static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
