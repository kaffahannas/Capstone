using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ============================================
    // TABEL: patients
    // ============================================
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Foreign Key ke AspNetUsers

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? EmployeeId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // 'male', 'female', 'other'
        public string EmploymentStatus { get; set; } = "active";
    }

    // ============================================
    // TABEL: psychologists
    // ============================================
    public class Psychologist
    {
        [Key]
        public int PsychologistId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Foreign Key ke AspNetUsers

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Bio { get; set; }
    }

    // ============================================
    // TABEL: hr_staff
    // ============================================
    public class HrStaff
    {
        [Key]
        public int HrId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Foreign Key ke AspNetUsers

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? Department { get; set; }
    }
}