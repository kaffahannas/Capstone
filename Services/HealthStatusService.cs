using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services
{
    // Computes MentalHealthStatus and Total Mood % from MoodTracker + JournalCheckIn data.
    // Called from ProfileController on read. Could later be a nightly hosted service.
    public class HealthStatusService
    {
        private readonly ApplicationDbContext _context;

        public HealthStatusService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Map MoodTracker.Feeling string → 1..5 (matches spec).
        private static int? MapFeelingScore(string feeling) => feeling switch
        {
            "Overjoyed" => 5,
            "Happy" => 4,
            "Calm" => 4,
            "Neutral" => 3,
            "Disappointed" => 2,
            "Angry" => 1,
            _ => null
        };

        public class Snapshot
        {
            public string Status { get; set; } = "Sehat";     // Sehat / Beresiko / Bahaya
            public int? TotalMoodPercent { get; set; }        // 0..100, null if no data
            public DateTime? LastCheckAt { get; set; }
            public string LastCheckLabel { get; set; } = "Belum ada";
        }

        public async Task<Snapshot> ComputeAsync(int patientId)
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-7);
            var thirtyDaysAgo = today.AddDays(-30);

            // ─── Pull data ───
            var moods7d = await _context.MoodTrackers
                .Where(m => m.PatientId == patientId && m.MoodDate >= sevenDaysAgo)
                .ToListAsync();

            var checkIns7d = await _context.JournalCheckIns
                .Where(c => c.PatientId == patientId && c.CheckInDate >= sevenDaysAgo)
                .ToListAsync();

            var moods30d = await _context.MoodTrackers
                .Where(m => m.PatientId == patientId && m.MoodDate >= thirtyDaysAgo)
                .ToListAsync();

            var checkIns30d = await _context.JournalCheckIns
                .Where(c => c.PatientId == patientId && c.CheckInDate >= thirtyDaysAgo)
                .ToListAsync();

            // ─── Status (7-day window) ───
            var samples7d = new List<int>();
            foreach (var m in moods7d)
            {
                var s = MapFeelingScore(m.Feeling);
                if (s != null) samples7d.Add(s.Value);
                
                // Add the questionnaire average from the same day
                if (m.FocusScore.HasValue)
                {
                    var allAverages = new[] { m.FocusScore.Value, m.AnxietyScore!.Value, m.SleepScore!.Value, m.MindLoadScore!.Value, m.EmotionScore!.Value };
                    samples7d.Add((int)Math.Round(allAverages.Average()));
                }
            }
            samples7d.AddRange(checkIns7d.Select(c => c.OverallScore)); // Keep for legacy check-ins

            string status = "Sehat";
            if (samples7d.Count > 0)
            {
                var avg = samples7d.Average();
                status = avg >= 4.0 ? "Sehat" : (avg >= 2.5 ? "Beresiko" : "Bahaya");
            }
            // No data → keep "Sehat" as the default (or whatever's currently saved).

            // ─── Total Mood % (30-day window) ───
            var samples30d = new List<int>();
            foreach (var m in moods30d)
            {
                var s = MapFeelingScore(m.Feeling);
                if (s != null) samples30d.Add(s.Value);
                
                // Add the questionnaire average from the same day
                if (m.FocusScore.HasValue)
                {
                    var allAverages = new[] { m.FocusScore.Value, m.AnxietyScore!.Value, m.SleepScore!.Value, m.MindLoadScore!.Value, m.EmotionScore!.Value };
                    samples30d.Add((int)Math.Round(allAverages.Average()));
                }
            }
            samples30d.AddRange(checkIns30d.Select(c => c.OverallScore)); // Keep for legacy check-ins

            int? percent = null;
            if (samples30d.Count > 0)
            {
                var avg30 = samples30d.Average();
                percent = (int)Math.Round(avg30 / 5.0 * 100.0);
            }

            // ─── Last check (latest of mood or check-in across all time) ───
            var lastMood = await _context.MoodTrackers
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.RecordedAt)
                .Select(m => (DateTime?)m.RecordedAt)
                .FirstOrDefaultAsync();
            var lastCheck = await _context.JournalCheckIns
                .Where(c => c.PatientId == patientId)
                .OrderByDescending(c => c.RecordedAt)
                .Select(c => (DateTime?)c.RecordedAt)
                .FirstOrDefaultAsync();

            DateTime? lastAt = null;
            if (lastMood.HasValue && lastCheck.HasValue) lastAt = lastMood > lastCheck ? lastMood : lastCheck;
            else lastAt = lastMood ?? lastCheck;

            string label = "Belum ada";
            if (lastAt.HasValue)
            {
                var d = lastAt.Value.Date;
                if (d == today) label = "Hari Ini";
                else if (d == today.AddDays(-1)) label = "Kemarin";
                else label = lastAt.Value.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            }

            return new Snapshot
            {
                Status = status,
                TotalMoodPercent = percent,
                LastCheckAt = lastAt,
                LastCheckLabel = label
            };
        }

        // Persist the computed status onto Patient.MentalHealthStatus.
        public async Task UpdateAndSaveAsync(Patient patient)
        {
            var snap = await ComputeAsync(patient.PatientId);
            if (patient.MentalHealthStatus != snap.Status)
            {
                patient.MentalHealthStatus = snap.Status;
                await _context.SaveChangesAsync();
            }
        }
    }
}
