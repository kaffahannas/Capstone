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

        // Personal (from edit profile)
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        // Emergency contact (from edit profile)
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelation { get; set; }

        // Kesehatan Mental
        public int? TotalMoodPercent { get; set; }
        public string LastCheckLabel { get; set; } = "Belum ada";

        // Sesi Konseling
        public DateTime? LastSessionAt { get; set; }
        public DateTime? NextSessionAt { get; set; }
        public bool RecommendSession => MentalHealthStatus == "Beresiko";


        // Support button targets
        public string? EmergencyContactEmail { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? CompanyHrEmail { get; set; }
        public string? PsychologistName { get; set; }
        public string? PsychologistEmail { get; set; }

        // Stats
        public int TotalSessionsDone { get; set; }
        public int TotalTasksDone { get; set; }
        public int MoodStreakDays { get; set; }   // consecutive days with mood logged
    }

    public class PatientProfileEditViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(32)]
        public string? Phone { get; set; }

        public Microsoft.AspNetCore.Http.IFormFile? ProfilePicture { get; set; }

        /// <summary>Existing photo URL for preview on the edit form.</summary>
        public string? CurrentProfilePicture { get; set; }

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


        // For display only
        public bool IsAlreadyB2B { get; set; }
        public string? CurrentCompanyName { get; set; }
    }
}
