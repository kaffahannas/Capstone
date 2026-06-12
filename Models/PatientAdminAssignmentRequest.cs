using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    /// <summary>Patient asks admin to assign a psychologist when they have no active one.</summary>
    public class PatientAdminAssignmentRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public int? PreferredPsychologistId { get; set; }
        [ForeignKey("PreferredPsychologistId")]
        public virtual Psychologist? PreferredPsychologist { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        /// <summary>Pending | Assigned | Dismissed</summary>
        [Required, StringLength(32)]
        public string Status { get; set; } = "Pending";

        public int? AssignedPsychologistId { get; set; }
        [ForeignKey("AssignedPsychologistId")]
        public virtual Psychologist? AssignedPsychologist { get; set; }

        public string? AssignedByAdminUserId { get; set; }
        [ForeignKey("AssignedByAdminUserId")]
        public virtual ApplicationUser? AssignedByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionAt { get; set; }
    }
}
