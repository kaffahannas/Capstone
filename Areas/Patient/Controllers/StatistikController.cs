using LightenUp.Web.Data;
using LightenUp.Web.Filters;
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
    [RequiresPatientPremium]
    public class StatistikController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatistikController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static double FeelingToScore(string feeling) => feeling switch
        {
            "Overjoyed" => 5,
            "Happy" => 4,
            "Calm" => 4,
            "Neutral" => 3,
            "Disappointed" => 2,
            "Angry" => 1,
            _ => 3
        };

        [HttpGet]
        public async Task<IActionResult> Index(int window = 30)
        {
            if (window != 7 && window != 30 && window != 90) window = 30;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return RedirectToAction("Welcome", "Onboarding");

            var today = DateTime.Today;
            var from = today.AddDays(-window);

            // ─── A: Mood trend ───
            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= from)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();
            var trend = moods.Select(m => new MoodTrendPoint
            {
                Date = m.MoodDate,
                Score = FeelingToScore(m.Feeling),
                Feeling = m.Feeling
            }).ToList();

            // ─── B: Check-in radar ───
            // We now get radar scores from MoodTrackers
            var moodScores = moods.Where(m => m.FocusScore.HasValue).ToList();
            var radar = new CheckInRadar();
            bool hasRadar = moodScores.Count > 0;
            if (hasRadar)
            {
                radar.Focus    = Math.Round(moodScores.Average(c => c.FocusScore!.Value), 1);
                radar.Anxiety  = Math.Round(moodScores.Average(c => c.AnxietyScore!.Value), 1);
                radar.Sleep    = Math.Round(moodScores.Average(c => c.SleepScore!.Value), 1);
                radar.MindLoad = Math.Round(moodScores.Average(c => c.MindLoadScore!.Value), 1);
                radar.Emotion  = Math.Round(moodScores.Average(c => c.EmotionScore!.Value), 1);
                var allAverages = new[] { radar.Focus, radar.Anxiety, radar.Sleep, radar.MindLoad, radar.Emotion };
                radar.Overall  = Math.Round(allAverages.Average(), 1);
            }


            // ─── D: Engagement (consecutive-days streak across mood OR check-in) ───
            var trackedDates = new HashSet<DateTime>(
                moods.Select(m => m.MoodDate.Date)
            );

            // Streak ending today (or yesterday if today not tracked yet)
            int currentStreak = 0;
            var probe = today;
            if (!trackedDates.Contains(probe)) probe = probe.AddDays(-1);
            while (trackedDates.Contains(probe))
            {
                currentStreak++;
                probe = probe.AddDays(-1);
            }

            // Longest streak — scan sorted unique dates
            int longest = 0;
            var sorted = trackedDates.OrderBy(d => d).ToList();
            int run = 0;
            DateTime? prev = null;
            foreach (var d in sorted)
            {
                if (prev == null || (d - prev.Value).TotalDays != 1) run = 1;
                else run++;
                if (run > longest) longest = run;
                prev = d;
            }

            var engagement = new EngagementStats
            {
                CurrentStreak = currentStreak,
                TotalDaysTracked = trackedDates.Count,
                LongestStreak = longest
            };

            // ─── F: Top triggers (one primary trigger per mood entry) ───
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var moodEntriesWithTriggers = 0;
            foreach (var m in moods.OrderBy(m => m.MoodDate))
            {
                var sessionTriggers = MoodOptions.ParseStoredTriggers(m.Triggers)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (sessionTriggers.Count == 0) continue;

                moodEntriesWithTriggers++;
                var primary = sessionTriggers[0];
                counts.TryGetValue(primary, out var n);
                counts[primary] = n + 1;
            }
            var topTriggers = counts
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new TriggerCount
                {
                    Trigger = kv.Key,
                    Label = MoodOptions.TriggerLabel(kv.Key),
                    Count = kv.Value
                })
                .ToList();

            var vm = new PatientStatistikViewModel
            {
                Window = window,
                MoodTrend = trend,
                Radar = radar,
                HasRadarData = hasRadar,
                Engagement = engagement,
                TopTriggers = topTriggers,
                MoodEntryCount = moods.Count,
                MoodEntriesWithTriggers = moodEntriesWithTriggers
            };

            ViewBag.ActiveNav = "Statistik";
            return View(vm);
        }
    }
}
