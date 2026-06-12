using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ─── HR slice 5 & Patient: requests to psychologist ─────
    // Created by HR ("+" buttons on /Hr/Worksheets and /Hr/Schedules)
    // OR by Patient (choose session via /Patient/Psychologists/RequestSession).
    public class PsychologistRequest
    {
        [Key]
        public int Id { get; set; }

        // HR requester (null when request is made by patient)
        public string? RequestedByHrUserId { get; set; }
        [ForeignKey("RequestedByHrUserId")]
        public virtual ApplicationUser? RequestedByHr { get; set; }

        // Patient requester (null when request is made by HR)
        public string? RequestedByPatientUserId { get; set; }
        [ForeignKey("RequestedByPatientUserId")]
        public virtual ApplicationUser? RequestedByPatient { get; set; }

        // "HR" or "Patient" — identifies who originated the request
        [StringLength(32)] public string RequesterRole { get; set; } = "HR";

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public int? PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required, StringLength(32)]
        public string RequestType { get; set; } = string.Empty;     // "Worksheet" or "Schedule"

        [StringLength(1000)] public string? Notes { get; set; }
        [StringLength(200)]  public string? ProposedTaskName { get; set; }
        public DateTime? ProposedDeadline { get; set; }
        public DateTime? ProposedSessionDate { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = "Pending";  // Pending / Approved / Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        [StringLength(500)] public string? RespondedNote { get; set; }
    }
}
