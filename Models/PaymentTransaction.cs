using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    /// <summary>DuitKu payment attempt (B2C patient or B2B company subscription).</summary>
    // #Class PaymentTransaction#
    public class PaymentTransaction
    {
        [Key]
        public int PaymentId { get; set; }

        public int? PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public int? SubscriptionId { get; set; }
        [ForeignKey("SubscriptionId")]
        public virtual Subscription? Subscription { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public int? CompanySubscriptionId { get; set; }
        [ForeignKey("CompanySubscriptionId")]
        public virtual CompanySubscription? CompanySubscription { get; set; }

        [Required, MaxLength(100)]
        public string MerchantOrderId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DuitkuReference { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        /// <summary>pending | paid | failed | expired</summary>
        [Required, MaxLength(32)]
        public string PaymentStatus { get; set; } = "pending";

        [MaxLength(10)]
        public string? ResultCode { get; set; }

        [MaxLength(255)]
        public string? ResultMessage { get; set; }

        public string? CallbackPayload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
