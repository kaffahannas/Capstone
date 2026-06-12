using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ─── HR slice 6 & Psy slice 9: escalation reports (bidirectional) ─────
    public class Report
    {
        [Key]
        public int Id { get; set; }

        // "HrToPsy" (default — HR escalates to psychologist)
        // "PsyToHr" (psychologist reports patient back to HR)
        [Required, StringLength(16)]
        public string Direction { get; set; } = "HrToPsy";

        // For HrToPsy: this is the sending HR. For PsyToHr: leave null (use ReportedByPsyUserId instead).
        public string? ReportedByHrUserId { get; set; }
        [ForeignKey("ReportedByHrUserId")]
        public virtual ApplicationUser? ReportedByHr { get; set; }

        // For PsyToHr: this is the sending psy. For HrToPsy: leave null.
        public string? ReportedByPsyUserId { get; set; }
        [ForeignKey("ReportedByPsyUserId")]
        public virtual ApplicationUser? ReportedByPsy { get; set; }

        // For PsyToHr: explicit HR recipient when there are multiple HRs in the company.
        public string? HrRecipientUserId { get; set; }
        [ForeignKey("HrRecipientUserId")]
        public virtual ApplicationUser? HrRecipient { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [StringLength(2000)] public string? Notes { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = "Draft";        // Draft / Sent

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EmailSentAt { get; set; }

        [StringLength(200)] public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
    }
}
