using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class Subscription#
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int MaxSessionsPerMonth { get; set; } = 4;

        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }
}
