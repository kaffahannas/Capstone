using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class Worksheet
    {
        [Key]
        public int WorksheetId { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime Deadline { get; set; }

        // Assigned (Belum Dikerjakan) → InProgress (Sedang Dikerjakan) → Completed (Selesai)
        public string Status { get; set; } = "Assigned";

        public string? ProofImagePath { get; set; }     // Patient's uploaded photo
        public string? Note { get; set; }               // Patient's "Deskripsikan Perasaanmu"
        public string? PsychologistFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }      // Set when patient first submits proof
        public DateTime? ReviewedAt { get; set; }       // Set when psychologist marks Completed

        public string? HrNote { get; set; }             // HR's note from /Hr/Worksheets/Review (slice 5)
    }
}
