using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class HrStaff
    {
        [Key]
        public int HrId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // HR pasti memiliki Perusahaan
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public string? Department { get; set; }       // Division this HR manages
        public string? EmployeeId { get; set; }       // HR's own ID at the company (e.g. "HR001")

        // ─── Onboarding survey ───
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? SupportDocumentUrl { get; set; }    // company supporting doc (e.g. SIPP scan)
        public DateTime? OnboardingCompletedAt { get; set; }

        // ─── Navigation ───

    }
}
