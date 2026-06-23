namespace LightenUp.Web.Models.ViewModels
{
    // 4 widgets: mood trend line, check-in radar, triggers bar, engagement streaks
    // #Class PatientStatistikViewModel#
    public class PatientStatistikViewModel
    {
        public int Window { get; set; } = 30;          // 7 / 30 / 90 days

        // Widget A — Mood trend
        public List<MoodTrendPoint> MoodTrend { get; set; } = new();

        public CheckInRadar Radar { get; set; } = new();
        public bool HasRadarData { get; set; }
        public bool HasCheckedInToday { get; set; }

        // Widget D — Engagement
        public EngagementStats Engagement { get; set; } = new();

        // Widget F — Triggers
        public List<TriggerCount> TopTriggers { get; set; } = new();
        public int MoodEntryCount { get; set; }
        public int MoodEntriesWithTriggers { get; set; }
    }

    // #Class MoodTrendPoint#
    public class MoodTrendPoint
    {
        public DateTime Date { get; set; }
        public double Score { get; set; }       // 1..5 mapped
        public string Feeling { get; set; } = string.Empty;
    }

    // #Class CheckInRadar#
    public class CheckInRadar
    {
        public double Focus { get; set; }
        public double Anxiety { get; set; }
        public double Sleep { get; set; }
        public double MindLoad { get; set; }
        public double Emotion { get; set; }
        public double Overall { get; set; }
    }

    // #Class EngagementStats#
    public class EngagementStats
    {
        public int CurrentStreak { get; set; }
        public int TotalDaysTracked { get; set; }
        public int LongestStreak { get; set; }
    }

    // #Class TriggerCount#
    public class TriggerCount
    {
        public string Trigger { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
