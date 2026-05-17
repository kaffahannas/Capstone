using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string view = "Mingguan")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null || patient.OnboardingCompletedAt == null)
            {
                return RedirectToAction("Welcome", "Onboarding");
            }

            // ─── Calendar window based on selected view (Harian / Mingguan / Bulanan) ───
            var today = DateTime.Today;
            DateTime windowStart, windowEnd;
            int dayCount;
            if (view == "Harian")
            {
                windowStart = today;
                windowEnd = today.AddDays(1);
                dayCount = 1;
            }
            else if (view == "Bulanan")
            {
                windowStart = new DateTime(today.Year, today.Month, 1);
                windowEnd = windowStart.AddMonths(1);
                dayCount = (windowEnd - windowStart).Days;
            }
            else
            {
                view = "Mingguan";
                int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
                windowStart = today.AddDays(-daysFromMonday);
                windowEnd = windowStart.AddDays(7);
                dayCount = 7;
            }

            var moodsInWindow = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= windowStart && m.MoodDate < windowEnd)
                .ToListAsync();

            var week = Enumerable.Range(0, dayCount).Select(i =>
            {
                var d = windowStart.AddDays(i);
                var mood = moodsInWindow.FirstOrDefault(m => m.MoodDate.Date == d.Date);
                return new CalendarDay
                {
                    Date = d,
                    IsToday = d.Date == today,
                    FeelingForDay = mood?.Feeling
                };
            }).ToList();

            var todayMood = moodsInWindow.FirstOrDefault(m => m.MoodDate.Date == today);
            ViewBag.CalendarView = view;

            // Latest journal entry (any date)
            var latestJournal = await _context.Journals
                .Where(j => j.PatientId == patient.PatientId)
                .OrderByDescending(j => j.JournalDate)
                .FirstOrDefaultAsync();

            // Has the patient done today's structured check-in?
            var hasCheckIn = await _context.JournalCheckIns
                .AnyAsync(c => c.PatientId == patient.PatientId && c.CheckInDate.Date == today);

            // Open tasks (Assigned or InProgress) — count + top 5 list for preview
            var openWorksheetsQ = _context.Worksheets
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .Where(w => w.PatientId == patient.PatientId && w.Status != "Completed");

            var openTaskCount = await openWorksheetsQ.CountAsync();

            var openWorksheets = await openWorksheetsQ
                .OrderBy(w => w.Deadline)
                .Take(5)
                .Select(w => new PatientWorksheetPreview
                {
                    WorksheetId = w.WorksheetId,
                    TaskName = w.TaskName,
                    Status = w.Status,
                    StatusLabel = w.Status == "Assigned" ? "Belum Dikerjakan"
                                : w.Status == "InProgress" ? "Sedang Dikerjakan"
                                : w.Status,
                    Deadline = w.Deadline,
                    PsychologistName = w.Psychologist!.User!.FullName
                })
                .ToListAsync();

            var monthName = today.ToString("MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));

            var vm = new PatientDashboardViewModel
            {
                MonthYearLabel = char.ToUpper(monthName[0]) + monthName.Substring(1),
                Week = week,
                TodayFeeling = todayMood?.Feeling,
                LatestJournalId = latestJournal?.JournalId,
                LatestJournalTitle = latestJournal?.Title,
                LatestJournalSnippet = latestJournal == null
                    ? null
                    : (latestJournal.Content.Length > 140
                        ? latestJournal.Content.Substring(0, 140) + "…"
                        : latestJournal.Content),
                HasTodayCheckIn = hasCheckIn,
                OpenTaskCount = openTaskCount,
                OpenWorksheets = openWorksheets
            };

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }
    }
}
