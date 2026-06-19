using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class Schedule#
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        // Combined date + time for the session start (was: SessionDate + SessionTime).
        // One column makes range queries (e.g. "sessions between 9am and 5pm Tuesday") simple.
        public DateTime SessionStart { get; set; }

        // Optional duration; defaults to 60 minutes.
        public int DurationMinutes { get; set; } = 60;

        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

        public string? MeetingLink { get; set; }
        public string? Notes { get; set; }

        // --- Audit Trail for Payroll ---
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AppliedPercentage { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal? SlotValue { get; set; }
    }
}
