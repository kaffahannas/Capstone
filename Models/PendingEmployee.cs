using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ─── HR slice 2: pre-registered employees ─────
    // HR adds expected employees here; when a patient registers with the company's referral code
    // and matching email, the row gets claimed and the patient's Dept/EmployeeId auto-fill.
    // #Class PendingEmployee#
    public class PendingEmployee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        public int? DivisionId { get; set; }
        [ForeignKey("DivisionId")]
        public virtual CompanyDivision? Division { get; set; }
        [StringLength(64)] public string? EmployeeId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ClaimedByPatientId { get; set; }
        [ForeignKey("ClaimedByPatientId")]
        public virtual Patient? ClaimedByPatient { get; set; }

        public DateTime? ClaimedAt { get; set; }
    }
}
