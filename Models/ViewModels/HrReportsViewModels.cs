using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    public class HrReportListItem
    {
        public int ReportId { get; set; }
        public string PatientName { get; set; } = "";
        public string PsychologistName { get; set; } = "";
        public string Status { get; set; } = "";   // Draft / Sent
        public DateTime CreatedAt { get; set; }
        public DateTime? EmailSentAt { get; set; }
    }

    public class HrReportListViewModel
    {
        public string Tab { get; set; } = "All";   // All / Draft / Sent
        public List<HrReportListItem> Items { get; set; } = new();
    }

    public class HrReportCreateViewModel
    {
        public int? ReportId { get; set; }  // null = new, else editing existing draft
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "";
        public string? PatientDepartment { get; set; }
        public string PatientStatus { get; set; } = "";  // Sehat / Beresiko / Bahaya

        public int PsychologistId { get; set; }
        public string PsychologistName { get; set; } = "";
        public string? PsychologistEmail { get; set; }

        // Computed metrics shown to HR
        public List<DateTime> MoodTrendDates { get; set; } = new();
        public List<double> MoodTrendScores { get; set; } = new();
        public string? MoodTrendLabel { get; set; }       // "Menurun" / "Stabil" / "Meningkat"
        public double AssignmentScore { get; set; }       // 0..10
        public string AssignmentTrend { get; set; } = ""; // "Menurun" etc
        public string StressLevel { get; set; } = "";    // "Rendah" / "Sedang" / "Tinggi"

        [StringLength(2000)]
        public string? Notes { get; set; }    // Editable note

        public string PreviewEmailSubject { get; set; } = "";
        public string PreviewEmailBody { get; set; } = "";
    }
}
