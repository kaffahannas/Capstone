using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ─── Payroll settings per psychologist (set by Admin) ─────
    // #Class PsychologistPayrollSetting#
    public class PsychologistPayrollSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        // Rate per completed counseling session (in IDR)
        [Required]
        public decimal SessionRate { get; set; } = 0;

        /// <summary>
        /// Default revenue share for this psychologist (e.g. 40 for 40%).
        /// Used as a fallback if a patient assignment doesn't snapshot it.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PsychologistPercentage { get; set; } = 40m;

        // --- Two-Way Approval Flow ---
        [Required, StringLength(32)]
        public string Status { get; set; } = "Active"; // Active, PendingPsyApproval, RejectedByPsy

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ProposedPercentage { get; set; }

        public string? AdminReason { get; set; }

        public string? UpdatedByAdminUserId { get; set; }
        [ForeignKey("UpdatedByAdminUserId")]
        public virtual ApplicationUser? UpdatedByAdmin { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Payroll Agreement Form (Bank details & Consent) ---
        [Required, StringLength(32)]
        public string AgreementStatus { get; set; } = "None"; // None, PendingAdmin, Approved

        [StringLength(500)]
        public string? BankDetailsPdfPath { get; set; }
    }
}
