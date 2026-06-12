using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class CompanyPsychologistRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        // Nullable: If null, the request is to Admin to find any suitable psychologist.
        public int? PsychologistId { get; set; }

        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required]
        [StringLength(32)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [StringLength(1000)]
        public string? Notes { get; set; } // Notes from HR

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedDate { get; set; }

        // Optional: If Admin processes this request, we could track who handled it
        public string? HandledByAdminUserId { get; set; }
        
        [ForeignKey("HandledByAdminUserId")]
        public virtual ApplicationUser? HandledByAdminUser { get; set; }
    }
}
