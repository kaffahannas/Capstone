using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services
{
// #Class HealthStatusService#
    // Menghitung MentalHealthStatus dan distribusi mood dari MoodTracker + JournalCheckIn.
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

        public class MoodDistribution
        {
            public int SehatPct { get; set; }
            public int BeresikoPct { get; set; }
            public int BahayaPct { get; set; }
        }

        public class MoodWindowResult
        {
            public List<string> Labels { get; set; } = new();
            public List<double?> Scores { get; set; } = new();
            public MoodDistribution Distribution { get; set; } = new();
            public bool HasData { get; set; }
        }

        private static IEnumerable<int> SamplesFromMood(MoodTracker m)
        {
            var samples = new List<int>();
            var feeling = MapFeelingScore(m.Feeling);
            if (feeling != null) samples.Add(feeling.Value);

            if (m.FocusScore.HasValue)
            {
                var questionnaireAvg = (int)Math.Round(new[]
                {
                    m.FocusScore.Value,
                    m.AnxietyScore!.Value,
                    m.SleepScore!.Value,
                    m.MindLoadScore!.Value,
                    m.EmotionScore!.Value
                }.Average());
                samples.Add(questionnaireAvg);
            }

            return samples;
        }

        private static double? DailyScore(DateTime day, List<MoodTracker> moods, List<JournalCheckIn> checkIns)
        {
            var samples = new List<int>();
            foreach (var mood in moods.Where(x => x.MoodDate.Date == day.Date))
                samples.AddRange(SamplesFromMood(mood));
            samples.AddRange(checkIns.Where(c => c.CheckInDate.Date == day.Date).Select(c => c.OverallScore));
            return samples.Count == 0 ? null : samples.Average();
        }

        private static void BucketScore(double score, ref int sehat, ref int beresiko, ref int bahaya)
        {
            if (score >= 4.0) sehat++;
            else if (score >= 2.5) beresiko++;
            else bahaya++;
        }

        private static MoodDistribution DistributionFromScores(IEnumerable<double?> scores)
        {
            int sehatN = 0, beresikoN = 0, bahayaN = 0;
            foreach (var score in scores.Where(x => x.HasValue).Select(x => x!.Value))
                BucketScore(score, ref sehatN, ref beresikoN, ref bahayaN);

            var totalN = sehatN + beresikoN + bahayaN;
            if (totalN == 0)
                return new MoodDistribution();

            return new MoodDistribution
            {
                SehatPct = (int)Math.Round((double)sehatN / totalN * 100),
                BeresikoPct = (int)Math.Round((double)beresikoN / totalN * 100),
                BahayaPct = (int)Math.Round((double)bahayaN / totalN * 100)
            };
        }

        // #Function ComputeAsync#
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
                samples7d.AddRange(SamplesFromMood(m));
            samples7d.AddRange(checkIns7d.Select(c => c.OverallScore));

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
                samples30d.AddRange(SamplesFromMood(m));
            samples30d.AddRange(checkIns30d.Select(c => c.OverallScore));

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

        // #Function ComputeMoodWindowAsync#
        public async Task<MoodWindowResult> ComputeMoodWindowAsync(int patientId, int days)
        {
            days = days is 7 or 30 or 90 ? days : 7;
            var from = DateTime.Today.AddDays(-(days - 1));

            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == patientId && m.MoodDate >= from)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            var checkIns = await _context.JournalCheckIns
                .Where(c => c.PatientId == patientId && c.CheckInDate >= from)
                .ToListAsync();

            List<string> labels;
            List<double?> scores;

            if (days <= 30)
            {
                var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
                labels = dates.Select(d => d.ToString("dd/MM")).ToList();
                scores = dates.Select(d => DailyScore(d, moods, checkIns)).ToList();
            }
            else
            {
                int weeks = (days / 7) + 1;
                var weekStarts = Enumerable.Range(0, weeks).Select(i => from.AddDays(i * 7)).ToList();
                labels = weekStarts.Select(w => w.ToString("dd/MM")).ToList();
                scores = weekStarts.Select(w =>
                {
                    var weekScores = Enumerable.Range(0, 7)
                        .Select(offset => DailyScore(w.AddDays(offset), moods, checkIns))
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value)
                        .ToList();
                    return weekScores.Count == 0 ? (double?)null : weekScores.Average();
                }).ToList();
            }

            return new MoodWindowResult
            {
                Labels = labels,
                Scores = scores,
                Distribution = DistributionFromScores(scores),
                HasData = scores.Any(x => x.HasValue)
            };
        }

        // #Function UpdateAndSaveAsync#
        public async Task UpdateAndSaveAsync(Patient patient)
        {
            var snap = await ComputeAsync(patient.PatientId);
            if (patient.MentalHealthStatus != snap.Status)
            {
                patient.MentalHealthStatus = snap.Status;
                await _context.SaveChangesAsync();
            }
        }

        // #Function RefreshStatusesAsync#
        public async Task RefreshStatusesAsync(IEnumerable<Patient> patients)
        {
            var changed = false;
            foreach (var patient in patients)
            {
                if (patient == null) continue;
                var snap = await ComputeAsync(patient.PatientId);
                if (patient.MentalHealthStatus != snap.Status)
                {
                    patient.MentalHealthStatus = snap.Status;
                    changed = true;
                }
            }

            if (changed)
                await _context.SaveChangesAsync();
        }
    }
}
