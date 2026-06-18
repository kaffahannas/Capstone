using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class PatientPsychologistAssignment#
    public class PatientPsychologistAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        // Jika B2C (Publik) = NULL. Jika B2B (Perusahaan) = ID User HR.
        // Now a real FK to AspNetUsers (configured in OnModelCreating).
        public string? AssignedByHrUserId { get; set; }
        [ForeignKey("AssignedByHrUserId")]
        public virtual ApplicationUser? AssignedByHr { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Status values:
        // PendingAdminApproval   — psychologist added patient; waiting for admin OK
        // PendingPsychologistApproval — patient chose psychologist; waiting psy to accept
        // Active                 — fully approved, live assignment
        // PendingCancellationByHr    — psy wants to end B2B partnership; HR must approve
        // PendingCancellationByAdmin — HR wants to remove employee's psy; Admin must approve
        // Cancelled              — assignment ended
        // Rejected               — approval denied
        public string Status { get; set; } = "Active";

        // Who requested this assignment and in what role ("Psychologist" / "Patient")
        public string? RequestedByUserId { get; set; }
        [ForeignKey("RequestedByUserId")]
        public virtual ApplicationUser? RequestedBy { get; set; }
        [StringLength(32)] public string? RequestedByRole { get; set; }

        // Cancellation request tracking
        public string? CancellationRequestedByUserId { get; set; }
        [ForeignKey("CancellationRequestedByUserId")]
        public virtual ApplicationUser? CancellationRequestedBy { get; set; }
        [StringLength(1000)] public string? CancellationReason { get; set; }
        public DateTime? CancellationRequestedAt { get; set; }

        // Decision (approve/reject) tracking
        public string? DecisionByUserId { get; set; }
        [ForeignKey("DecisionByUserId")]
        public virtual ApplicationUser? DecisionBy { get; set; }
        public DateTime? DecisionAt { get; set; }
        [StringLength(1000)] public string? DecisionNote { get; set; }

        /// <summary>Monthly subscription slot value (IDR) snapshotted when assignment becomes Active.</summary>
        [Column(TypeName = "decimal(14,2)")]
        public decimal? SlotValue { get; set; }

        /// <summary>Max sessions allowed per month snapshotted from the patient's subscription.</summary>
        public int? MaxSessionsPerMonth { get; set; }

        /// <summary>Psychologist revenue share % of SlotValue (e.g. 40 = 40%).</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? PsychologistRevenuePercentage { get; set; }
    }
}
