using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // #Class HrEmployeeListItem#
    public class HrEmployeeListItem
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? Department { get; set; }
        public string? ProfilePicture { get; set; }
        public string Status { get; set; } = "Sehat";   // Sehat / Beresiko / Bahaya
        public DateTime? OnboardingCompletedAt { get; set; }

        // For "Sesi Hari Ini" tab
        public DateTime? TodaySessionStart { get; set; }
        public int? TodaySessionDurationMinutes { get; set; }
        public string? TodaySessionTitle { get; set; }   // "General Counseling" etc — not in schema yet; use Schedule.Notes or default
    }

    // #Class HrEmployeesListViewModel#
    public class HrEmployeesListViewModel
    {
        public string Tab { get; set; } = "Semua";       // "Semua" or "Sesi"
        public string? Search { get; set; }
        public string? Division { get; set; }
        public string? Status { get; set; }
        public List<string> Divisions { get; set; } = new();
        public List<HrEmployeeListItem> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
    }

    // #Class HrEmployeeDetailViewModel#
    public class HrEmployeeDetailViewModel
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string Age { get; set; } = "—";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }
        public string Status { get; set; } = "Sehat";
        public string? Symptoms { get; set; }            // Patient.Symptoms (editable inline)

        public string? TodayJournalSnippet { get; set; }
        public string? TodayJournalKeluhan { get; set; }

        // Mood chart data (Week or Month)
        public string ChartWindow { get; set; } = "Week";  // "Week" or "Month"
        public List<DateTime> ChartDates { get; set; } = new();
        public List<double> ChartScores { get; set; } = new();

        // Status percentages over the chart window
        public int SehatPct { get; set; }
        public int BeresikoPct { get; set; }
        public int BahayaPct { get; set; }

        // Next session preview
        public DateTime? NextSessionStart { get; set; }
        public string? NextSessionNote { get; set; }

        // Open worksheets count
        public int OpenWorksheetCount { get; set; }

        // Active psychologist assignment (for HR cancel request)
        public int? ActiveAssignmentId { get; set; }
        public string? AssignedPsychologistName { get; set; }
        public string? AssignmentStatus { get; set; }
    }

    // #Class HrEditSymptomsViewModel#
    public class HrEditSymptomsViewModel
    {
        public int PatientId { get; set; }
        [StringLength(500)]
        public string? Symptoms { get; set; }
    }

    // #Class HrAddClientViewModel#
    public class HrAddClientViewModel
    {
        [Required(ErrorMessage = "Divisi wajib dipilih.")]
        public int DivisionId { get; set; }

        [Required(ErrorMessage = "Nama karyawan wajib diisi.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email karyawan wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(64)]
        public string? EmployeeId { get; set; }
    }
}
