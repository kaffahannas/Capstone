using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class Journal
    {
        [Key]
        public int JournalId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        // The day this journal entry is for. Unique per (PatientId, JournalDate).
        public DateTime JournalDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
