using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── Worksheets list ───
    public class HrWorksheetListItem
    {
        public int WorksheetId { get; set; }
        public string PatientName { get; set; } = "";
        public string TaskName { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string DateLabel { get; set; } = "";
        public string PsychologistName { get; set; } = "";
    }

    public class HrWorksheetListViewModel
    {
        public string? Search { get; set; }
        public List<string> Statuses { get; set; } = new();
        public string? Period { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public List<HrWorksheetListItem> Items { get; set; } = new();
    }

    public class HrWorksheetReviewViewModel
    {
        public int WorksheetId { get; set; }
        public string PatientName { get; set; } = "";
        public string? PatientEmail { get; set; }
        public string? PsychologistEmail { get; set; }
        public string PsychologistName { get; set; } = "";
        public string TaskName { get; set; } = "";
        public string? Description { get; set; }
        public string Status { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public DateTime? SubmittedAt { get; set; }
        public string? ProofImagePath { get; set; }
        public string? PatientNote { get; set; }
        public string? PsychologistFeedback { get; set; }
        public string? HrNote { get; set; }
    }

    public class HrWorksheetEditNoteViewModel
    {
        public int WorksheetId { get; set; }
        [StringLength(500)]
        public string? HrNote { get; set; }
    }

    // ─── Schedules list + edit ───
    public class HrScheduleListItem
    {
        public int ScheduleId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "";
        public DateTime SessionStart { get; set; }
        public int DurationMinutes { get; set; }
        public string DbStatus { get; set; } = "";       // raw: Scheduled / Completed / Cancelled
        public string DisplayStatus { get; set; } = "";  // computed: Akan Datang / On Going / Selesai / Dibatalkan
        public string? Notes { get; set; }
        public string PsychologistName { get; set; } = "";
    }

    public class HrScheduleListViewModel
    {
        public string? Search { get; set; }
        public string? Period { get; set; }   // HariIni / Mingguan / Bulanan / Tahunan
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public List<HrScheduleListItem> Items { get; set; } = new();
    }

    public class HrScheduleEditViewModel
    {
        public int ScheduleId { get; set; }
        public string PatientName { get; set; } = "";
        public string PsychologistName { get; set; } = "";

        [Required]
        public DateTime SessionStart { get; set; }
        [Range(15, 240)]
        public int DurationMinutes { get; set; } = 60;
        [Required]
        public string Status { get; set; } = "Scheduled";
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // ─── Request to psychologist (worksheet or schedule) ───
    public class HrRequestViewModel
    {
        [Required]
        public string RequestType { get; set; } = "";    // "Worksheet" or "Schedule"

        [Required(ErrorMessage = "Pilih pasien.")]
        public int PatientId { get; set; }

        public List<HrSimplePatient> AvailablePatients { get; set; } = new();

        [StringLength(1000)]
        public string? Notes { get; set; }

        // For Worksheet request
        [StringLength(200)]
        public string? ProposedTaskName { get; set; }
        public DateTime? ProposedDeadline { get; set; }

        // For Schedule request
        public DateTime? ProposedSessionDate { get; set; }
    }

    public class HrSimplePatient
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = "";
        public string? Department { get; set; }
    }
}
