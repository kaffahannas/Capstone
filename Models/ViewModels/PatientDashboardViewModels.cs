using LightenUp.Web.Models;

namespace LightenUp.Web.Models.ViewModels
{
    // One day on the weekly calendar strip.
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsToday { get; set; }
        public string? FeelingForDay { get; set; }  // null if no mood logged
        public string DayOfWeekShort => Date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Sen",
            DayOfWeek.Tuesday => "Sel",
            DayOfWeek.Wednesday => "Rab",
            DayOfWeek.Thursday => "Kam",
            DayOfWeek.Friday => "Jum",
            DayOfWeek.Saturday => "Sab",
            _ => "Min"
        };
    }

    public class PatientDashboardViewModel
    {
        public string MonthYearLabel { get; set; } = string.Empty;     // "Oktober 2024"
        public List<CalendarDay> Week { get; set; } = new();

        // Mood card
        public string? TodayFeeling { get; set; }            // null = empty state

        // Journal card — show latest entry's title + content snippet (any date)
        public int? LatestJournalId { get; set; }
        public string? LatestJournalTitle { get; set; }
        public string? LatestJournalSnippet { get; set; }
        public bool HasTodayCheckIn { get; set; }

        // Tasks card
        public int OpenTaskCount { get; set; }
    }

    // Lightweight enum-string helpers used in views.
    public static class MoodPalette
    {
        public static string CssClass(string? feeling) => feeling?.ToLowerInvariant() ?? "";
        public static string Emoji(string? feeling) => feeling switch
        {
            "Overjoyed" => "😆",
            "Happy" => "😊",
            "Calm" => "😌",
            "Neutral" => "😐",
            "Disappointed" => "🙁",
            "Angry" => "😠",
            _ => "🙂"
        };
        public static string Label(string? feeling) => feeling ?? "";
    }
}
