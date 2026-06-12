using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    public class HrEmployeeRemovalRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public string? RequestedByHrUserId { get; set; }
        [ForeignKey("RequestedByHrUserId")]
        public virtual ApplicationUser? RequestedByHr { get; set; }

        [StringLength(1000)]
        public string? Reason { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? DecisionByAdminUserId { get; set; }
        [ForeignKey("DecisionByAdminUserId")]
        public virtual ApplicationUser? DecisionByAdmin { get; set; }

        public DateTime? DecisionAt { get; set; }
        [StringLength(1000)]
        public string? DecisionNote { get; set; }
    }
}
