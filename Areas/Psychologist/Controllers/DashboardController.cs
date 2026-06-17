using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    [Authorize(Roles = "Psychologist")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionPricingService _pricing;
        private readonly HealthStatusService _healthService;
        private readonly AssignmentActivationService _activation;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SubscriptionPricingService pricing,
            HealthStatusService healthService,
            AssignmentActivationService activation)
        {
            _context = context;
            _userManager = userManager;
            _pricing = pricing;
            _healthService = healthService;
            _activation = activation;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var psychologist = await _context.Psychologists
                .Include(p => p.PartneredCompanies)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (psychologist == null) return NotFound("Data Psikolog tidak ditemukan.");

            var payrollSetting = await _context.PayrollSettings.FirstOrDefaultAsync(ps => ps.PsychologistId == psychologist.PsychologistId);
            if (payrollSetting == null || payrollSetting.AgreementStatus == "None")
            {
                ViewBag.ShowPayrollAgreement = true;
            }

            await _activation.RepairDuplicateLiveAssignmentsAsync(psychologist.PsychologistId);

            var activeAssignments = AssignmentActivationService.SelectPrimaryPerPatient(
                await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psychologist.PsychologistId && AssignmentActivationService.LiveClientListStatuses.Contains(a.Status))
                .ToListAsync());

            var activePatients = activeAssignments.Select(a => a.Patient!).ToList();

            var pendingAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psychologist.PsychologistId &&
                            (a.Status == "PendingAdminApproval" || a.Status == "PendingPsychologistApproval"))
                .ToListAsync();

            var partnerCompanies = psychologist.PartneredCompanies.ToList();
            var partnerCompanyIds = partnerCompanies.Select(c => c.CompanyId).ToList();

            var unassignedPatientsDb = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .Where(p => !_context.Assignments.Any(a => a.PatientId == p.PatientId &&
                    (a.Status == "Active" || a.Status == "PendingAdminApproval" || a.Status == "PendingPsychologistApproval")))
                .Where(p => p.CompanyId == null || partnerCompanyIds.Contains(p.CompanyId.Value))
                .ToListAsync();

            await _healthService.RefreshStatusesAsync(
                activePatients.Concat(unassignedPatientsDb));

            var pendingB2B = await _context.CompanyPsychologistRequests
                .Include(r => r.Company)
                .Where(r => r.PsychologistId == psychologist.PsychologistId && r.Status == "Pending")
                .ToListAsync();

            var viewModel = new LightenUp.Web.Models.ViewModels.PsychologistDashboardViewModel
            {
                PsychologistName = user.FullName ?? "Psikolog",
                TotalClients = activePatients.Count,
                PendingB2BRequests = pendingB2B,
                Patients = activeAssignments.Select(a => new LightenUp.Web.Models.ViewModels.PatientListItem
                {
                    PatientId = a.Patient?.PatientId ?? 0,
                    FullName = a.Patient?.User?.FullName ?? "Anonim",
                    Gender = a.Patient?.Gender ?? "-",
                    JoinedDate = a.AssignedAt,
                    Status = a.Patient?.MentalHealthStatus ?? "Sehat",
                    CompanyId = a.Patient?.CompanyId,
                    CompanyName = a.Patient?.Company?.Name ?? "Publik",
                    AssignmentId = a.AssignmentId
                }).ToList(),
                PendingAssignments = pendingAssignments.Select(a => new LightenUp.Web.Models.ViewModels.PatientListItem
                {
                    PatientId = a.Patient?.PatientId ?? 0,
                    FullName = a.Patient?.User?.FullName ?? "Anonim",
                    Gender = a.Patient?.Gender ?? "-",
                    Status = a.Status,
                    CompanyId = a.Patient?.CompanyId,
                    CompanyName = a.Patient?.Company?.Name ?? "Publik",
                    AssignmentId = a.AssignmentId
                }).ToList(),
                PartnerCompanies = partnerCompanies,
                UnassignedPatients = unassignedPatientsDb.Select(p => new LightenUp.Web.Models.ViewModels.PatientListItem
                {
                    PatientId = p.PatientId,
                    FullName = p.User?.FullName ?? "Anonim",
                    Gender = p.Gender ?? "-",
                    Status = p.MentalHealthStatus ?? "Sehat",
                    CompanyId = p.CompanyId,
                    CompanyName = p.Company?.Name ?? "Publik"
                }).ToList()
            };

            return View(viewModel);
        }

        private async Task<int?> CurrentPsychologistIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.PsychologistId).FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            await _activation.RepairDuplicateLiveAssignmentsAsync(psyId.Value);

            var assignedIds = AssignmentActivationService.SelectPrimaryPerPatient(
                await _context.Assignments
                .Where(a => a.PsychologistId == psyId && AssignmentActivationService.LiveClientListStatuses.Contains(a.Status))
                .ToListAsync())
                .Select(a => a.PatientId)
                .ToList();

            var patients = await _context.Patients
                .Where(p => assignedIds.Contains(p.PatientId))
                .ToListAsync();

            var viewModel = new LightenUp.Web.Models.ViewModels.StatisticsViewModel
            {
                TotalClients = patients.Count,
                HealthyCount = patients.Count(p => p.MentalHealthStatus == "Sehat"),
                AtRiskCount  = patients.Count(p => p.MentalHealthStatus == "Beresiko"),
                DangerCount  = patients.Count(p => p.MentalHealthStatus == "Bahaya")
            };

            var byCompany = patients.Where(p => p.CompanyId != null)
                .GroupBy(p => p.CompanyId!.Value)
                .ToList();
            var companyIds = byCompany.Select(g => g.Key).ToList();
            var companyMap = await _context.Companies
                .Where(c => companyIds.Contains(c.CompanyId))
                .ToDictionaryAsync(c => c.CompanyId, c => c.Name);

            var divisions = byCompany.Select(g =>
            {
                var total = g.Count();
                var s = g.Count(p => p.MentalHealthStatus == "Sehat");
                var b = g.Count(p => p.MentalHealthStatus == "Beresiko");
                var d = g.Count(p => p.MentalHealthStatus == "Bahaya");
                return new DivisionRow
                {
                    CompanyId = g.Key,
                    Name = companyMap.GetValueOrDefault(g.Key, "—"),
                    Total = total,
                    SehatPct = total == 0 ? 0 : (int)Math.Round((double)s / total * 100),
                    StressPct = total == 0 ? 0 : (int)Math.Round((double)(b + d) / total * 100)
                };
            }).OrderBy(x => x.Name).ToList();

            ViewBag.Divisions = divisions;
            return View(viewModel);
        }

        public class DivisionRow
        {
            public int CompanyId { get; set; }
            public string Name { get; set; } = "";
            public int Total { get; set; }
            public int SehatPct { get; set; }
            public int StressPct { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Payslip()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            await _activation.RepairDuplicateLiveAssignmentsAsync(psych.PsychologistId);

            var activeAssignments = AssignmentActivationService.SelectPrimaryPerPatient(
                await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psych.PsychologistId && AssignmentActivationService.LiveClientListStatuses.Contains(a.Status))
                .ToListAsync());

            var setting = await _context.PayrollSettings.FirstOrDefaultAsync(s => s.PsychologistId == psych.PsychologistId);

            var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var nextMonth = firstOfMonth.AddMonths(1);

            var completedSchedules = await _context.Schedules
                .Where(s => s.PsychologistId == psych.PsychologistId &&
                            s.Status == "Completed" &&
                            s.SessionStart >= firstOfMonth &&
                            s.SessionStart < nextMonth)
                .GroupBy(s => s.PatientId)
                .Select(g => new { PatientId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PatientId, x => x.Count);

            var subscriptionTotal = 0m;
            var psyShareThisMonth = 0m;

            var assignmentRows = new List<LightenUp.Web.Models.ViewModels.PayslipAssignmentRow>();
            foreach (var a in activeAssignments)
            {
                completedSchedules.TryGetValue(a.PatientId, out int completedCount);

                var pricing = await _pricing.GetPatientPricingAsync(a.PatientId);
                decimal subscriptionValue = pricing.SubscriptionValuePerMonth;
                int maxSessions = pricing.MaxSessions;
                decimal perSessionValue = pricing.PerSessionValue;

                if (subscriptionValue <= 0 && a.SlotValue > 0)
                {
                    subscriptionValue = a.SlotValue ?? 0;
                    maxSessions = a.MaxSessionsPerMonth ?? maxSessions;
                    perSessionValue = maxSessions > 0
                        ? Math.Round(subscriptionValue / maxSessions, 2, MidpointRounding.AwayFromZero)
                        : 0;
                }

                decimal feePercent = a.PsychologistRevenuePercentage ?? (setting?.PsychologistPercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage);
                decimal psyShare = perSessionValue * (feePercent / 100m) * completedCount;

                subscriptionTotal += subscriptionValue;
                psyShareThisMonth += psyShare;

                assignmentRows.Add(new LightenUp.Web.Models.ViewModels.PayslipAssignmentRow
                {
                    PatientName = a.Patient?.User?.FullName ?? "Unknown",
                    PatientType = pricing.IsB2B ? "B2B" : "B2C",
                    CompletedSessions = completedCount,
                    MaxSessions = maxSessions,
                    SubscriptionValue = subscriptionValue,
                    B2BPlanAmount = pricing.B2BPlanAmount,
                    B2BEmployeeCount = pricing.B2BEmployeeCount,
                    PerSessionValue = perSessionValue,
                    FeePercentage = feePercent,
                    PsyShare = psyShare
                });
            }

            var model = new LightenUp.Web.Models.ViewModels.PayslipViewModel
            {
                MonthName = firstOfMonth.ToString("MMMM yyyy"),
                TotalGross = subscriptionTotal,
                TotalPsyShare = psyShareThisMonth,
                TotalFeePercent = setting?.PsychologistPercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage,
                Assignments = assignmentRows.OrderByDescending(r => r.PsyShare).ToList(),
                PayoutStatus = "Pending" // Will be updated by scheduled jobs
            };

            var payouts = await _context.MonthlyPayouts
                .Where(p => p.PsychologistId == psych.PsychologistId)
                .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
                .ToListAsync();

            ViewBag.PayrollSetting = setting;
            ViewBag.Payouts = payouts;

            return View("~/Areas/Psychologist/Views/Dashboard/Payslip.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePartnership(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists
                .Include(p => p.PartneredCompanies)
                .FirstOrDefaultAsync(p => p.UserId == user!.Id);

            if (psych == null) return NotFound();

            var req = await _context.CompanyPsychologistRequests
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == psych.PsychologistId && r.Status == "Pending");

            if (req == null) return NotFound();

            req.Status = "Approved";
            req.RespondedDate = DateTime.UtcNow;

            if (req.Company != null && !psych.PartneredCompanies.Any(c => c.CompanyId == req.CompanyId))
            {
                psych.PartneredCompanies.Add(req.Company);
            }

            await _context.SaveChangesAsync();
            TempData["success"] = $"Kemitraan dengan {req.Company?.Name} telah disetujui.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RejectPartnership(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user!.Id);

            if (psych == null) return NotFound();

            var req = await _context.CompanyPsychologistRequests
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == psych.PsychologistId && r.Status == "Pending");

            if (req == null) return NotFound();

            req.Status = "Rejected";
            req.RespondedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = $"Permintaan kemitraan dari {req.Company?.Name} ditolak.";

            return RedirectToAction(nameof(Index));
        }
    }
}
