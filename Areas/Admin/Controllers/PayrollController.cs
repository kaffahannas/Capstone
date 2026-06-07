using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PayrollController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psychologists = await _context.Psychologists
                .Include(p => p.User)
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            var settingsMap = await _context.PayrollSettings
                .ToDictionaryAsync(s => s.PsychologistId);

            var activeAssignments = await _context.Assignments
                .Where(a => a.Status == "Active" && a.SlotValue != null && a.PsychologistRevenuePercentage != null)
                .ToListAsync();

            // Count completed sessions this calendar month for each psychologist
            var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var nextMonth = firstOfMonth.AddMonths(1);

            var completedSessionsThisMonth = await _context.Schedules
                .Where(s => s.Status == "Completed" &&
                            s.SessionStart >= firstOfMonth &&
                            s.SessionStart < nextMonth)
                .GroupBy(s => s.PsychologistId)
                .Select(g => new { PsyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PsyId, x => x.Count);

            var rows = psychologists.Select(p =>
            {
                settingsMap.TryGetValue(p.PsychologistId, out var settings);
                var mine = activeAssignments.Where(a => a.PsychologistId == p.PsychologistId).ToList();
                var grossTotal = mine.Sum(a => a.SlotValue ?? 0);

                // Per-session model (Sir Lukas): SlotValue / MaxSessions * PsyPercentage%
                var maxSessions = settings?.MaxSessionsPerMonth > 0 ? settings.MaxSessionsPerMonth : 4;
                var psyPct = settings?.PsychologistPercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage;

                var perSessionValue = mine.Sum(a =>
                {
                    var slotVal = a.SlotValue ?? 0;
                    if (slotVal <= 0) return 0m;
                    return Math.Round(slotVal / maxSessions, 2, MidpointRounding.AwayFromZero);
                });

                completedSessionsThisMonth.TryGetValue(p.PsychologistId, out var completedSessions);
                var psyEarningsThisMonth = perSessionValue * completedSessions * psyPct / 100;

                return new AdminPayrollRow
                {
                    PsychologistId = p.PsychologistId,
                    FullName = p.User?.FullName ?? "—",
                    Email = p.User?.Email ?? "—",
                    DefaultPsychologistPercentage = psyPct,
                    MaxSessionsPerMonth = maxSessions,
                    ActivePatients = mine.Count,
                    CompletedSessionsThisMonth = completedSessions,
                    GrossMonthly = grossTotal,
                    PerSessionValueTotal = perSessionValue,
                    PsyShareMonthly = psyEarningsThisMonth
                };
            }).ToList();

            ViewBag.ActiveNav = "Payroll";
            ViewData["Title"] = "Payroll Psikolog";
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var psy = await _context.Psychologists.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PsychologistId == id);
            if (psy == null) return NotFound();

            var setting = await _context.PayrollSettings
                .FirstOrDefaultAsync(s => s.PsychologistId == id)
                ?? new PsychologistPayrollSetting
                {
                    PsychologistId = id,
                    PsychologistPercentage = SubscriptionPricingService.DefaultPsychologistRevenuePercentage
                };

            ViewBag.PsyName = psy.User?.FullName ?? "—";
            ViewBag.ActiveNav = "Payroll";
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PsychologistPayrollSetting model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                var psy = await _context.Psychologists.Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PsychologistId == model.PsychologistId);
                ViewBag.PsyName = psy?.User?.FullName ?? "—";
                ViewBag.ActiveNav = "Payroll";
                return View(model);
            }

            var existing = await _context.PayrollSettings
                .FirstOrDefaultAsync(s => s.PsychologistId == model.PsychologistId);

            if (existing == null)
            {
                existing = new PsychologistPayrollSetting { PsychologistId = model.PsychologistId };
                _context.PayrollSettings.Add(existing);
            }

            existing.PsychologistPercentage = model.PsychologistPercentage;
            existing.MaxSessionsPerMonth = model.MaxSessionsPerMonth > 0 ? model.MaxSessionsPerMonth : 4;
            existing.UpdatedByAdminUserId = user?.Id;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = "Pengaturan default persentase disimpan.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class AdminPayrollRow
    {
        public int PsychologistId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal DefaultPsychologistPercentage { get; set; }
        public int MaxSessionsPerMonth { get; set; } = 4;
        public int ActivePatients { get; set; }
        public int CompletedSessionsThisMonth { get; set; }
        public decimal GrossMonthly { get; set; }
        public decimal PerSessionValueTotal { get; set; }
        public decimal PsyShareMonthly { get; set; }
    }
}
