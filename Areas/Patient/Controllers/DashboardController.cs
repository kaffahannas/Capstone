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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null || patient.OnboardingCompletedAt == null)
            {
                // Onboarding not done — bounce back.
                return RedirectToAction("Welcome", "Onboarding");
            }

            // Week strip = Monday-anchored ISO week containing today.
            var today = DateTime.Today;
            int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
            var monday = today.AddDays(-daysFromMonday);

            var weekEnd = monday.AddDays(7);
            var moodsThisWeek = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= monday && m.MoodDate < weekEnd)
                .ToListAsync();

            var week = Enumerable.Range(0, 7).Select(i =>
            {
                var d = monday.AddDays(i);
                var mood = moodsThisWeek.FirstOrDefault(m => m.MoodDate.Date == d.Date);
                return new CalendarDay
                {
                    Date = d,
                    IsToday = d.Date == today,
                    FeelingForDay = mood?.Feeling
                };
            }).ToList();

            var todayMood = moodsThisWeek.FirstOrDefault(m => m.MoodDate.Date == today);

            // Latest journal entry (any date)
            var latestJournal = await _context.Journals
                .Where(j => j.PatientId == patient.PatientId)
                .OrderByDescending(j => j.JournalDate)
                .FirstOrDefaultAsync();

            // Has the patient done today's structured check-in?
            var hasCheckIn = await _context.JournalCheckIns
                .AnyAsync(c => c.PatientId == patient.PatientId && c.CheckInDate.Date == today);

            // Open tasks (Assigned or InProgress)
            var openTaskCount = await _context.Worksheets
                .CountAsync(w => w.PatientId == patient.PatientId && w.Status != "Completed");

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
                OpenTaskCount = openTaskCount
            };

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }
    }
}
