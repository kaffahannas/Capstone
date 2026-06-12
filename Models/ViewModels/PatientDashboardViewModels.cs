using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;

namespace LightenUp.Web.Models.ViewModels
{
    // One day on the weekly calendar strip.
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsToday { get; set; }
        public string? FeelingForDay { get; set; }  // null if no mood logged
        public bool HasDeadline { get; set; }       // task deadline on this day
        public bool HasKonseling { get; set; }      // counseling session on this day
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
        public bool IsSubscribed { get; set; }
        public string MonthYearLabel { get; set; } = string.Empty;     // "Oktober 2024"
        public List<CalendarDay> Week { get; set; } = new();

        // Mood card
        public string? TodayFeeling { get; set; }            // null = empty state
        public MoodTracker? TodayMoodData { get; set; }

        // Journal card — show latest entry's title + content snippet (any date)
        public int? LatestJournalId { get; set; }
        public string? LatestJournalTitle { get; set; }
        public string? LatestJournalContent { get; set; }
        public string? LatestJournalSnippet { get; set; }
        public bool HasTodayCheckIn { get; set; }

        // Tasks card
        public int OpenTaskCount { get; set; }
        public List<PatientWorksheetPreview> OpenWorksheets { get; set; } = new();

        // Jadwal card
        public List<JadwalItemViewModel> UpcomingSessions { get; set; } = new();
        public int UpcomingSessionCount { get; set; }  // KPI for hero banner
    }

    public class PatientWorksheetPreview
    {
        public int WorksheetId { get; set; }
        public string TaskName { get; set; } = "";
        public string Status { get; set; } = "";       // Assigned / InProgress / Completed
        public string StatusLabel { get; set; } = "";  // Belum Dikerjakan / Sedang Dikerjakan / Selesai
        public DateTime Deadline { get; set; }
        public string PsychologistName { get; set; } = "";
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
