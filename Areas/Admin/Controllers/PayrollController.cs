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
    // #Class PayrollController#
    [Authorize(Roles = "Admin")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;
        private readonly SubscriptionPricingService _pricing;

        public PayrollController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            UserUploadService uploads,
            SubscriptionPricingService pricing)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
            _pricing = pricing;
        }

        // #Function Index#

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
                .Where(a => a.Status == "Active")
                .ToListAsync();

            // Count completed sessions and calculate earnings this calendar month for each psychologist
            var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var nextMonth = firstOfMonth.AddMonths(1);

            var schedulesThisMonth = await _context.Schedules
                .Where(s => s.Status == "Completed" &&
                            s.SessionStart >= firstOfMonth &&
                            s.SessionStart < nextMonth)
                .ToListAsync();

            var assignmentsList = activeAssignments;

            var patientIds = assignmentsList.Select(a => a.PatientId)
                .Concat(schedulesThisMonth.Select(s => s.PatientId))
                .Distinct()
                .ToList();

            var pricingCache = new Dictionary<int, PatientPricingResult>();
            foreach (var patientId in patientIds)
                pricingCache[patientId] = await _pricing.GetPatientPricingAsync(patientId);

            var earningsMap = new Dictionary<int, decimal>();
            var countMap = new Dictionary<int, int>();

            foreach (var p in psychologists)
            {
                earningsMap[p.PsychologistId] = 0;
                countMap[p.PsychologistId] = 0;
            }

            foreach (var sched in schedulesThisMonth)
            {
                if (!earningsMap.ContainsKey(sched.PsychologistId)) continue;

                countMap[sched.PsychologistId]++;

                var assignment = assignmentsList.FirstOrDefault(a =>
                    a.PatientId == sched.PatientId && a.PsychologistId == sched.PsychologistId);
                var (subscriptionValue, maxSessions) = ResolvePatientSlot(
                    sched.PatientId, assignment, pricingCache);
                var pct = sched.AppliedPercentage
                    ?? assignment?.PsychologistRevenuePercentage
                    ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage;

                if (subscriptionValue > 0 && maxSessions > 0)
                {
                    var perSessionValue = Math.Round(subscriptionValue / maxSessions, 2, MidpointRounding.AwayFromZero);
                    earningsMap[sched.PsychologistId] += perSessionValue * pct / 100;
                }
            }

            var rows = psychologists.Select(p =>
            {
                settingsMap.TryGetValue(p.PsychologistId, out var settings);
                var mine = activeAssignments.Where(a => a.PsychologistId == p.PsychologistId).ToList();
                var grossTotal = mine.Sum(a =>
                {
                    var (subscriptionValue, _) = ResolvePatientSlot(a.PatientId, a, pricingCache);
                    return subscriptionValue;
                });

                var psyPct = settings?.PsychologistPercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage;

                return new AdminPayrollRow
                {
                    PsychologistId = p.PsychologistId,
                    FullName = p.User?.FullName ?? "—",
                    Email = p.User?.Email ?? "—",
                    DefaultPsychologistPercentage = psyPct,
                    MaxSessionsPerMonth = 4,
                    ActivePatients = mine.Count,
                    CompletedSessionsThisMonth = countMap[p.PsychologistId],
                    GrossMonthly = grossTotal,
                    PsyShareMonthly = earningsMap[p.PsychologistId],
                    Status = settings?.Status ?? "Belum Diatur"
                };
            }).ToList();

            ViewBag.ActiveNav = "Payroll";
            ViewData["Title"] = "Payroll Psikolog";
            return View(rows);
        }

        // #Function Edit#

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

            var payouts = await _context.MonthlyPayouts
                .Where(p => p.PsychologistId == id)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            ViewBag.PsyName = psy.User?.FullName ?? "—";
            ViewBag.ActiveNav = "Payroll";
            ViewBag.Payouts = payouts;
            return View(setting);
        }

        // #Function Edit POST#

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

            if (existing.PsychologistPercentage != model.PsychologistPercentage && string.IsNullOrWhiteSpace(model.AdminReason))
            {
                ModelState.AddModelError("AdminReason", "Alasan wajib diisi jika persentase diubah.");
                var psy = await _context.Psychologists.Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PsychologistId == model.PsychologistId);
                ViewBag.PsyName = psy?.User?.FullName ?? "—";
                ViewBag.ActiveNav = "Payroll";
                return View(model);
            }

            if (existing.PsychologistPercentage != model.PsychologistPercentage)
            {
                // Absolute admin power: apply directly, reset proposal state
                existing.PsychologistPercentage = model.PsychologistPercentage;
                existing.ProposedPercentage = null;
                existing.Status = "Active";
            }
            
            existing.AdminReason = model.AdminReason;
            existing.UpdatedByAdminUserId = user?.Id;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = "Pengaturan default persentase disimpan.";
            return RedirectToAction(nameof(Index));
        }

        // #Function Transfer#

        [HttpGet]
        public async Task<IActionResult> Transfer(int id, int? month, int? year)
        {
            var psy = await _context.Psychologists.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PsychologistId == id);
            if (psy == null) return NotFound();

            var setting = await _context.PayrollSettings
                .FirstOrDefaultAsync(s => s.PsychologistId == id);

            var payouts = await _context.MonthlyPayouts
                .Where(p => p.PsychologistId == id)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            // Calculate Breakdown for Selected Month & Year
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var firstOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var nextMonth = firstOfMonth.AddMonths(1);

            var schedules = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Where(s => s.PsychologistId == id && 
                            s.Status == "Completed" &&
                            s.SessionStart >= firstOfMonth &&
                            s.SessionStart < nextMonth)
                .ToListAsync();

            var assignmentsList = await _context.Assignments
                .Where(a => a.PsychologistId == id && a.Status == "Active")
                .ToListAsync();

            var pricingCache = new Dictionary<int, PatientPricingResult>();
            foreach (var patientId in schedules.Select(s => s.PatientId).Distinct())
                pricingCache[patientId] = await _pricing.GetPatientPricingAsync(patientId);

            var breakdown = new List<dynamic>();
            decimal totalCalculatedAmount = 0;

            var grouped = schedules.GroupBy(s => s.PatientId);
            foreach (var g in grouped)
            {
                var schedCount = g.Count();
                var firstSched = g.First();

                var assignment = assignmentsList.FirstOrDefault(a => a.PatientId == g.Key);
                var (subscriptionValue, maxSessions) = ResolvePatientSlot(g.Key, assignment, pricingCache);
                var pct = firstSched.AppliedPercentage
                    ?? assignment?.PsychologistRevenuePercentage
                    ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage;

                // Klien Klinik (SponsorType=Psychologist) dan B2C tanpa subscription tidak masuk transfer
                if (subscriptionValue <= 0) continue;

                decimal patientTotalEarning = 0;
                if (maxSessions > 0)
                {
                    var perSessionValue = Math.Round(subscriptionValue / maxSessions, 2, MidpointRounding.AwayFromZero);
                    patientTotalEarning = perSessionValue * pct / 100 * schedCount;
                    totalCalculatedAmount += patientTotalEarning;
                }

                breakdown.Add(new {
                    PatientName = firstSched.Patient?.User?.FullName ?? "Unknown",
                    Sessions = schedCount,
                    Schedules = g.ToList(),
                    SlotValue = subscriptionValue,
                    MaxSessions = maxSessions,
                    Percentage = pct,
                    Earning = patientTotalEarning
                });
            }

            ViewBag.PsyName = psy.User?.FullName ?? "—";
            ViewBag.ActiveNav = "Payroll";
            ViewBag.Payouts = payouts;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.Breakdown = breakdown;
            ViewBag.TotalCalculatedAmount = totalCalculatedAmount;
            
            return View(setting ?? new PsychologistPayrollSetting { PsychologistId = id });
        }

        // #Function UploadPayoutProof#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPayoutProof(int psychologistId, int month, int year, decimal amount, Microsoft.AspNetCore.Http.IFormFile ProofDocument)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var psy = await _context.Psychologists.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PsychologistId == psychologistId);
            if (psy == null || psy.UserId == null) return NotFound();

            if (ProofDocument != null && ProofDocument.Length > 0)
            {
                string path = await _uploads.SaveAsync(psy.UserId, "payouts", ProofDocument);
                
                var payout = new MonthlyPayout
                {
                    PsychologistId = psychologistId,
                    Month = month,
                    Year = year,
                    TotalAmount = amount,
                    ProofOfTransferFilePath = path,
                    Status = "Paid",
                    PaidAt = DateTime.UtcNow
                };

                _context.MonthlyPayouts.Add(payout);
                await _context.SaveChangesAsync();
                
                TempData["success"] = $"Bukti transfer bulan {month}/{year} berhasil diunggah.";
            }

            return RedirectToAction(nameof(Transfer), new { id = psychologistId });
        }

        private static (decimal SubscriptionValue, int MaxSessions) ResolvePatientSlot(
            int patientId,
            PatientPsychologistAssignment? assignment,
            IReadOnlyDictionary<int, PatientPricingResult> pricingCache)
        {
            if (pricingCache.TryGetValue(patientId, out var pricing) && pricing.SubscriptionValuePerMonth > 0)
                return (pricing.SubscriptionValuePerMonth, pricing.MaxSessions);

            if (assignment?.SlotValue is > 0)
            {
                return (
                    assignment.SlotValue.Value,
                    assignment.MaxSessionsPerMonth ?? 4);
            }

            return (0, pricingCache.TryGetValue(patientId, out var fallback) ? fallback.MaxSessions : 4);
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
        public string Status { get; set; } = string.Empty;
    }
}
