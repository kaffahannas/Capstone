using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // 6-question structured daily check-in (separate from free-write Journal)
    public class JournalCheckIn
    {
        [Key]
        public int CheckInId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Range(1, 5)] public int FocusScore { get; set; }        // Q1
        [Range(1, 5)] public int AnxietyScore { get; set; }      // Q2  (5 = least anxious)
        [Range(1, 5)] public int SleepScore { get; set; }        // Q3
        [Range(1, 5)] public int MindLoadScore { get; set; }     // Q4  (5 = least burdened)
        [Range(1, 5)] public int EmotionScore { get; set; }      // Q5
        [Range(1, 5)] public int OverallScore { get; set; }      // Q6

        public DateTime CheckInDate { get; set; }                // Unique per (PatientId, CheckInDate)
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
