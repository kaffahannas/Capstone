using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class Psychologist
    {
        [Key]
        public int PsychologistId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Bio { get; set; }

        // Data Onboarding (Asli milik Anda)
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? SiapNumber { get; set; }
        public string? StrDocumentUrl { get; set; }
        public string? PracticeLocation { get; set; }

        // --- NAVIGATION PROPERTIES (RELASI) ---
        public virtual ICollection<PatientPsychologistAssignment> Assignments { get; set; } = new List<PatientPsychologistAssignment>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<Worksheet> Worksheets { get; set; } = new List<Worksheet>();

        // Many-to-Many to Perusahaan
        public virtual ICollection<Company> PartneredCompanies { get; set; } = new List<Company>();

        // Opt-in to be visible in HR company directory (slice 6)
        public bool AcceptsB2B { get; set; } = false;

        // ─── Psychologist profile additions (Psy slice 10) ───
        public DateTime? OnboardingCompletedAt { get; set; }
        public string? AvailabilityText { get; set; }      // "Mon-Fri: 9AM-5PM"
        public bool IsAvailable { get; set; } = true;
        public string? OfficeAddress { get; set; }

        // ─── 1-to-1 notification preferences (Psy slice 10) ───

    }
}
