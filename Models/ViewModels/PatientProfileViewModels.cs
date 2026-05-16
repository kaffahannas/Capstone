using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    public class PatientProfileViewModel
    {
        // Header
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string MentalHealthStatus { get; set; } = "Sehat";   // Sehat / Beresiko / Bahaya

        // Work Info
        public bool IsB2B { get; set; }
        public string? Department { get; set; }
        public string? EmployeeId { get; set; }
        public string? CompanyName { get; set; }

        // Contact Info
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Kesehatan Mental
        public int? TotalMoodPercent { get; set; }
        public string LastCheckLabel { get; set; } = "Belum ada";

        // Sesi Konseling
        public DateTime? LastSessionAt { get; set; }
        public DateTime? NextSessionAt { get; set; }
        public bool RecommendSession => MentalHealthStatus == "Beresiko";

        // Notification preferences
        public bool RemindMoodCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowHrPsychologistNotif { get; set; } = true;
        public string ReminderTime { get; set; } = "09:00";

        // Support button targets
        public string? EmergencyContactEmail { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? CompanyHrEmail { get; set; }
        public string? PsychologistEmail { get; set; }
    }

    public class PatientProfileEditViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(32)]
        public string? Phone { get; set; }

        public Microsoft.AspNetCore.Http.IFormFile? ProfilePicture { get; set; }

        // Work Info (B2B only — null fields display as empty for B2C)
        [StringLength(64)] public string? Department { get; set; }
        [StringLength(64)] public string? EmployeeId { get; set; }

        // Personal
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        // Referral code — patient can join a company from here.
        [StringLength(64)]
        public string? ReferralCode { get; set; }

        // Emergency contact
        [StringLength(100)] public string? EmergencyContactName { get; set; }
        [StringLength(32)] public string? EmergencyContactPhone { get; set; }
        [StringLength(256)][EmailAddress] public string? EmergencyContactEmail { get; set; }
        [StringLength(32)] public string? EmergencyContactRelation { get; set; }

        // Notification prefs
        public bool RemindMoodCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowHrPsychologistNotif { get; set; } = true;
        public TimeSpan ReminderTime { get; set; } = new TimeSpan(9, 0, 0);

        // For display only
        public bool IsAlreadyB2B { get; set; }
        public string? CurrentCompanyName { get; set; }
    }
}
