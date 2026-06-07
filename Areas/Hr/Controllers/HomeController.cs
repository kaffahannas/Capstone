using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionAccessService _access;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _access = access;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var hr = await _context.HrStaffs
                .Include(h => h.Company)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
            if (hr == null || hr.OnboardingCompletedAt == null)
                return RedirectToAction("Welcome", "Onboarding");

            var companyId = hr.CompanyId ?? 0;

            // ─── Company-scoped queries ───
            var patientsQ = _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active");

            var activeCount = await patientsQ.CountAsync();
            var sehat       = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Sehat");
            var beresiko    = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Beresiko");
            var bahaya      = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Bahaya");

            var divisions = await patientsQ
                .Where(p => p.Department != null && p.Department != "")
                .Select(p => p.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var preview = await patientsQ
                .OrderBy(p => p.User!.FullName)
                .Take(20)
                .Select(p => new HrClientPreview
                {
                    PatientId  = p.PatientId,
                    FullName   = p.User!.FullName,
                    Department = p.Department,
                    Status     = p.MentalHealthStatus
                })
                .ToListAsync();

            // ─── Primary partnered psychologist (for "Kontak Psikolog" mailto) ───
            var partneredPsys = await _context.Companies
                .Where(c => c.CompanyId == companyId)
                .SelectMany(c => c.PartneredPsychologists)
                .Include(p => p.User)
                .OrderBy(p => p.PsychologistId)
                .Take(3)
                .Select(p => new HrPsyPreview
                {
                    Id = p.PsychologistId,
                    Name = p.User!.FullName,
                    Specialization = p.Specialization,
                    Years = p.ExperienceYears,
                    Email = p.User.Email,
                    IsPartner = true
                })
                .ToListAsync();
            
            var psyEmail = partneredPsys.FirstOrDefault()?.Email;

            var hasSub = hr.CompanyId != null && await _access.HasCompanyActiveSubscriptionAsync(hr.CompanyId.Value);

            // ─── Activity metrics ───
            var today     = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;            // 0=Sun … 6=Sat
            var weekStart = today.AddDays(dayOfWeek == 0 ? -6 : -(dayOfWeek - 1));  // ISO Monday
            var from7     = today.AddDays(-7);

            int todaySessions    = 0;
            int weekSessions     = 0;
            int activeWorksheets = 0;
            int pendingRequests  = 0;
            var recentActivities = new List<HrRecentActivity>();

            if (companyId != 0)
            {
                todaySessions = await _context.Schedules
                    .CountAsync(s => s.Patient!.CompanyId == companyId
                                  && s.SessionStart >= today && s.SessionStart < today.AddDays(1));

                weekSessions = await _context.Schedules
                    .CountAsync(s => s.Patient!.CompanyId == companyId
                                  && s.SessionStart >= weekStart && s.SessionStart < weekStart.AddDays(7));

                activeWorksheets = await _context.Worksheets
                    .CountAsync(w => w.Patient!.CompanyId == companyId
                                  && (w.Status == "Assigned" || w.Status == "InProgress"));

                pendingRequests = await _context.PsychologistRequests
                    .CountAsync(r => r.RequestedByHrUserId == user.Id && r.Status == "Pending");

                // Recent activity feed — completed worksheets + completed sessions (last 7 days)
                var rawWorksheets = await _context.Worksheets
                    .Include(w => w.Patient).ThenInclude(p => p!.User)
                    .Where(w => w.Patient!.CompanyId == companyId
                             && w.Status == "Completed" && w.ReviewedAt >= from7)
                    .OrderByDescending(w => w.ReviewedAt)
                    .Take(5)
                    .ToListAsync();

                var rawSessions = await _context.Schedules
                    .Include(s => s.Patient).ThenInclude(p => p!.User)
                    .Where(s => s.Patient!.CompanyId == companyId
                             && s.Status == "Completed" && s.SessionStart >= from7)
                    .OrderByDescending(s => s.SessionStart)
                    .Take(5)
                    .ToListAsync();

                recentActivities = rawWorksheets
                    .Select(w => new HrRecentActivity
                    {
                        Type        = "Worksheet",
                        PatientName = w.Patient?.User?.FullName ?? "—",
                        Description = $"Worksheet \"{w.TaskName}\" selesai direview",
                        OccurredAt  = w.ReviewedAt!.Value
                    })
                    .Concat(rawSessions.Select(s => new HrRecentActivity
                    {
                        Type        = "Session",
                        PatientName = s.Patient?.User?.FullName ?? "—",
                        Description = "Sesi konseling selesai",
                        OccurredAt  = s.SessionStart
                    }))
                    .OrderByDescending(a => a.OccurredAt)
                    .Take(6)
                    .ToList();
            }

            var vm = new HrDashboardViewModel
            {
                HrName               = user.FullName,
                CompanyName          = hr.Company?.Name ?? "",
                ActiveCount          = activeCount,
                SehatCount           = sehat,
                BeresikoCount        = beresiko,
                BahayaCount          = bahaya,
                Divisions            = divisions,
                ClientsPreview       = preview,
                PartneredPsychologists = partneredPsys,
                PrimaryPsychologistEmail = psyEmail,
                HasActiveSubscription    = hasSub,
                TodaySessionCount    = todaySessions,
                WeekSessionCount     = weekSessions,
                ActiveWorksheetCount = activeWorksheets,
                PendingRequestCount  = pendingRequests,
                RecentActivities     = recentActivities,
            };

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }

        // ─── AJAX: psychologist search (no subscription guard) ───
        [HttpGet]
        public async Task<IActionResult> SearchPsychologists(string? q)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var hr = await _context.HrStaffs
                .Include(h => h.Company).ThenInclude(c => c!.PartneredPsychologists)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
            if (hr == null) return Json(Array.Empty<object>());

            var partneredIds = hr.Company?.PartneredPsychologists
                                   .Select(p => p.PsychologistId).ToHashSet()
                               ?? new HashSet<int>();

            var query = _context.Psychologists
                .Include(p => p.User)
                .Where(p => p.AcceptsB2B && p.User != null && p.User.IsApprovedByAdmin);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p =>
                    p.User!.FullName.Contains(q) ||
                    (p.Specialization != null && p.Specialization.Contains(q)));

            var results = await query
                .OrderBy(p => p.User!.FullName)
                .Take(6)
                .Select(p => new
                {
                    id           = p.PsychologistId,
                    name         = p.User!.FullName,
                    specialization = p.Specialization,
                    years        = p.ExperienceYears,
                    email        = p.User.Email,
                    partnered    = partneredIds.Contains(p.PsychologistId)
                })
                .ToListAsync();

            return Json(results);
        }
    }
}
