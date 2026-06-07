using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    public class PsyAddScheduleViewModel
    {
        [Required(ErrorMessage = "Pilih pasien.")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Tanggal wajib diisi.")]
        public DateTime SessionDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Jam wajib diisi.")]
        public TimeSpan SessionTime { get; set; } = new TimeSpan(9, 0, 0);

        [Range(15, 240)]
        public int DurationMinutes { get; set; } = 60;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Url(ErrorMessage = "Link tidak valid. Pastikan diawali dengan http:// atau https://")]
        [StringLength(500)]
        public string? MeetingLink { get; set; }

        public List<PsyPatientOption> AvailablePatients { get; set; } = new();

        /// <summary>Filter tab to restore after submit (Scheduling page).</summary>
        public string ReturnFilter { get; set; } = "Semua";

        /// <summary>When set, success/error returns to PatientScheduleHistory for this patient.</summary>
        public int? ReturnPatientId { get; set; }
    }

    public class PsyAddTaskViewModel
    {
        [Required(ErrorMessage = "Pilih pasien.")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Nama tugas wajib diisi.")]
        [StringLength(200)]
        public string TaskName { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tanggal deadline wajib diisi.")]
        public DateTime DeadlineDate { get; set; } = DateTime.Today.AddDays(7);

        public TimeSpan DeadlineTime { get; set; } = new TimeSpan(23, 59, 0);

        public List<PsyPatientOption> AvailablePatients { get; set; } = new();

        /// <summary>When set, success/error returns to PatientWorksheetHistory for this patient.</summary>
        public int? ReturnPatientId { get; set; }
    }

    public class PsyWorksheetEditViewModel
    {
        [Required]
        public int WorksheetId { get; set; }

        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Nama tugas wajib diisi.")]
        [StringLength(200)]
        public string TaskName { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Tanggal deadline wajib diisi.")]
        public DateTime DeadlineDate { get; set; } = DateTime.Today.AddDays(7);

        public TimeSpan DeadlineTime { get; set; } = new TimeSpan(23, 59, 0);
    }

    public class PsyPatientOption
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = "";
        public string? CompanyName { get; set; }
    }

    public class PsyProfileExtViewModel
    {
        // Wraps PsychologistProfileViewModel + extra real-time stats
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public string? Specialization { get; set; }
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? PracticeLocation { get; set; }
        public string? OfficeAddress { get; set; }
        public string? SiapNumber { get; set; }
        public string? SippNumber { get; set; }
        public int? ExperienceYears { get; set; }
        public bool IsActive { get; set; } = true;

        // Availability
        public string AvailabilityText { get; set; } = "Mon-Fri: 9AM-5PM";
        public bool IsAvailable { get; set; } = true;

        // Workload
        public int Employees { get; set; }
        public int ActiveCases { get; set; }

        // B2B setting
        public bool AcceptsB2B { get; set; }

        // Notification preferences
        public bool RemindNewReports { get; set; } = true;
        public bool RemindFollowUp { get; set; } = true;
        public bool AllowHrPatientNotif { get; set; } = false;
        public string Frequency { get; set; } = "Daily";
    }

    public class EditProfileViewModel
    {
        [Required]
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
        public IFormFile? ProfilePictureFile { get; set; }
        public string? Bio { get; set; }
        public string? Specialization { get; set; }
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? PracticeLocation { get; set; }
        public string? OfficeAddress { get; set; }
        public string? SippNumber { get; set; }
        public int? ExperienceYears { get; set; }
    }
}
