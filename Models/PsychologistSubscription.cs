using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    /// <summary>Langganan add-on Mitra untuk psikolog yang ingin monitor pasien kliniknya sendiri.</summary>
    public class PsychologistSubscription
    {
        [Key]
        public int PsychologistSubscriptionId { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>Jumlah maksimal pasien klinik yang bisa dimonitor.</summary>
        public int PatientLimit { get; set; } = 50;

        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }
}
