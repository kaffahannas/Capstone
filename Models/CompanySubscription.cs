using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    /// <summary>B2B company plan purchased by HR; activates referral code for employees.</summary>
    // #Class CompanySubscription#
    public class CompanySubscription
    {
        [Key]
        public int CompanySubscriptionId { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>Max employees covered by this plan (0 = unlimited legacy).</summary>
        public int EmployeeLimit { get; set; }

        public int MaxSessionsPerMonth { get; set; } = 4;

        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }
}
