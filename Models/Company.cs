using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class Company#
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }               // For "Kontak HRD" mailto: link on Patient profile
        public string? RegistrationNumber { get; set; }         // "Nomor Perusahaan" — free-form business registration

        public string? ReferralCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<HrStaff> HrStaffs { get; set; } = new List<HrStaff>();
        public virtual ICollection<CompanySubscription> Subscriptions { get; set; } = new List<CompanySubscription>();
        public virtual ICollection<CompanyDivision> Divisions { get; set; } = new List<CompanyDivision>();

        // Many-to-Many to Psychologist
        public virtual ICollection<Psychologist> PartneredPsychologists { get; set; } = new List<Psychologist>();
    }
}
