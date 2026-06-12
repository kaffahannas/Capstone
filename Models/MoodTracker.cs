using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class MoodTracker
    {
        [Key]
        public int MoodId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        // One of: Overjoyed, Happy, Calm, Neutral, Disappointed, Angry
        [Required]
        [MaxLength(16)]
        public string Feeling { get; set; } = string.Empty;

        // CSV of triggers, e.g. "Work,Family,Hobby"
        // Valid values: Self, Family, School, Work, Friends, Partner, Hobby, Activity, SocialMedia, Entertainment
        [MaxLength(256)]
        public string Triggers { get; set; } = string.Empty;

        // Optional free-text reflection (mood-tracker step 2.5)
        [MaxLength(500)]
        public string? Note { get; set; }

        // 5-question wellbeing questionnaire (score 1-5 each), filled after Note step.
        public int? FocusScore     { get; set; }   // Q1: Fokus
        public int? AnxietyScore   { get; set; }   // Q2: Kecemasan
        public int? SleepScore     { get; set; }   // Q3: Tidur
        public int? MindLoadScore  { get; set; }   // Q4: Beban pikiran
        public int? EmotionScore   { get; set; }   // Q5: Emosi

        // The day this mood is for. Enforced unique per (PatientId, MoodDate) via OnModelCreating.
        public DateTime MoodDate { get; set; }

        // When the record was last created/updated.
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
