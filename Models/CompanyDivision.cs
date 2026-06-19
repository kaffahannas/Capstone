using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class CompanyDivision#
    public class CompanyDivision
    {
        [Key]
        public int DivisionId { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The unique referral code for this specific division
        public string? ReferralCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
