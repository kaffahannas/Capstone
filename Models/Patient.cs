using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class Patient#
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // --- B2B/B2C ---
        // Null = Pasien Publik (B2C). Ada isi = Karyawan (B2B).
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        // --- Sponsor (siapa yang menanggung sesi 1-on-1) ---
        // "Self" = B2C bayar sendiri, "Company" = B2B ditanggung HR, "Psychologist" = pasien klinik Mitra
        public string SponsorType { get; set; } = "Self";
        public int? SponsorPsychologistId { get; set; }
        [ForeignKey("SponsorPsychologistId")]
        public virtual Psychologist? SponsorPsychologist { get; set; }

        public string? EmployeeId { get; set; }
        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public virtual CompanyDivision? Division { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }                     // Male / Female
        public string EmploymentStatus { get; set; } = "active";

        // --- Mental-health status (computed from MoodTracker + JournalCheckIn) ---
        public string MentalHealthStatus { get; set; } = "Sehat"; // Sehat, Beresiko, Bahaya

        // Free-text symptoms/catatan shown on HR employee detail page (slice 3)
        public string? Symptoms { get; set; }

        // --- 14-step onboarding survey answers ---
        public string? RelationshipStatus { get; set; }         // Single, Dating, Married, Divorced, PreferNotToSay
        public string? SpiritualActivity { get; set; }          // Active, Rare, Inactive
        public bool? HasPreviousCounseling { get; set; }
        public string? CounselingMethods { get; set; }          // CSV: "CBT,Hypnotherapy,..."
        public string? CounselingMethodOther { get; set; }      // free text for "Lainnya"
        public bool? HasMedicationHistory { get; set; }
        public string? SleepQuality { get; set; }               // VeryGood, Average, Poor, Bad, VeryBad
        public string? AppGoals { get; set; }                   // CSV: "MoodTracking,Journaling,PersonalGoals,Unsure"
        public DateTime? TermsAcceptedAt { get; set; }
        public DateTime? OnboardingCompletedAt { get; set; }    // null until step 16 confirmed

        // --- Emergency contact (Profile page) ---
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactEmail { get; set; }
        public string? EmergencyContactRelation { get; set; }   // "Pasangan", "Saudara", etc.

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<PatientPsychologistAssignment> Assignments { get; set; } = new List<PatientPsychologistAssignment>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<Worksheet> Worksheets { get; set; } = new List<Worksheet>();
        public virtual ICollection<MoodTracker> MoodLogs { get; set; } = new List<MoodTracker>();
        public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();
        public virtual ICollection<JournalCheckIn> JournalCheckIns { get; set; } = new List<JournalCheckIn>();

    }
}
