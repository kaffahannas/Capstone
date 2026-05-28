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

        public async Task<IActionResult> Index(string view = "Mingguan", int offset = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients
                .Include(p => p.Schedules)
                    .ThenInclude(s => s.Psychologist)
                        .ThenInclude(psy => psy!.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null || patient.OnboardingCompletedAt == null)
            {
                return RedirectToAction("Welcome", "Onboarding");
            }

            // ─── Calendar window based on selected view + offset ───
            var today = DateTime.Today;
            DateTime windowStart, windowEnd;
            int dayCount;
            if (view == "Harian")
            {
                windowStart = today.AddDays(offset);
                windowEnd = windowStart.AddDays(1);
                dayCount = 1;
            }
            else if (view == "Bulanan")
            {
                windowStart = new DateTime(today.Year, today.Month, 1).AddMonths(offset);
                windowEnd = windowStart.AddMonths(1);
                dayCount = (windowEnd - windowStart).Days;
            }
            else
            {
                view = "Mingguan";
                int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
                windowStart = today.AddDays(-daysFromMonday).AddDays(offset * 7);
                windowEnd = windowStart.AddDays(7);
                dayCount = 7;
            }
            ViewBag.CalendarView = view;
            ViewBag.CalendarOffset = offset;

            var moodsInWindow = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= windowStart && m.MoodDate < windowEnd)
                .ToListAsync();

            // Deadlines in window
            var deadlinesInWindow = await _context.Worksheets
                .Where(w => w.PatientId == patient.PatientId && w.Deadline >= windowStart && w.Deadline < windowEnd)
                .Select(w => w.Deadline.Date)
                .ToListAsync();

            // Konseling sessions in window
            var konselingInWindow = (patient.Schedules ?? new List<Schedule>())
                .Where(s => s.SessionStart >= windowStart && s.SessionStart < windowEnd)
                .Select(s => s.SessionStart.Date)
                .ToList();

            var week = Enumerable.Range(0, dayCount).Select(i =>
            {
                var d = windowStart.AddDays(i);
                var mood = moodsInWindow.FirstOrDefault(m => m.MoodDate.Date == d.Date);
                return new CalendarDay
                {
                    Date = d,
                    IsToday = d.Date == today,
                    FeelingForDay = mood?.Feeling,
                    HasDeadline = deadlinesInWindow.Contains(d.Date),
                    HasKonseling = konselingInWindow.Contains(d.Date)
                };
            }).ToList();

            // Today's mood is always fetched independently of the calendar window
            var todayMood = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today)
                .OrderByDescending(m => m.MoodDate)
                .FirstOrDefaultAsync();
            var monthLabel = windowStart.ToString("MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            ViewBag.CalendarMonthLabel = char.ToUpper(monthLabel[0]) + monthLabel.Substring(1);

            // Latest journal entry (for today only)
            var latestJournal = await _context.Journals
                .Where(j => j.PatientId == patient.PatientId && j.JournalDate.Date == today)
                .OrderByDescending(j => j.JournalDate)
                .FirstOrDefaultAsync();

            // Has the patient done today's structured check-in (mood + questionnaire)?
            var hasCheckIn = await _context.MoodTrackers
                .AnyAsync(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today && m.FocusScore.HasValue);

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

            // Upcoming counseling sessions (top 3)
            var upcomingSessions = (patient.Schedules ?? new List<Schedule>())
                .Where(s => s.SessionStart >= today)
                .OrderBy(s => s.SessionStart)
                .Take(3)
                .Select(s => new JadwalItemViewModel
                {
                    ScheduleId = s.ScheduleId,
                    PsychologistName = s.Psychologist?.User?.FullName ?? "Psikolog",
                    SessionStart = s.SessionStart,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status,
                    MeetingLink = s.MeetingLink
                })
                .ToList();

            // Total upcoming session count for hero KPI
            var upcomingSessionCount = (patient.Schedules ?? new List<Schedule>())
                .Count(s => s.SessionStart >= today);

            var monthName = today.ToString("MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));

            var vm = new PatientDashboardViewModel
            {
                MonthYearLabel = char.ToUpper(monthName[0]) + monthName.Substring(1),
                Week = week,
                TodayFeeling = todayMood?.Feeling,
                LatestJournalId = latestJournal?.JournalId,
                LatestJournalTitle = latestJournal?.Title,
                LatestJournalContent = latestJournal?.Content,
                LatestJournalSnippet = latestJournal == null
                    ? null
                    : (latestJournal.Content.Length > 140
                        ? latestJournal.Content.Substring(0, 140) + "…"
                        : latestJournal.Content),
                HasTodayCheckIn = hasCheckIn,
                OpenTaskCount = openTaskCount,
                OpenWorksheets = openWorksheets,
                UpcomingSessions = upcomingSessions,
                UpcomingSessionCount = upcomingSessionCount
            };

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }
    }
}
