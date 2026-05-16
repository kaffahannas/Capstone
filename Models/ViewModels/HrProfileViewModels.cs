using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LightenUp.Web.Models.ViewModels
{
    public class HrProfileViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string CompanyName { get; set; } = "";
        public string? ReferralCode { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; }

        // Notification preferences
        public bool RemindEmployeeCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowEmployeePsyNotif { get; set; } = false;
        public string Frequency { get; set; } = "Daily";
    }

    public class HrProfileEditViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [StringLength(32)]
        public string? Phone { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        [StringLength(64)]
        public string? EmployeeId { get; set; }

        [StringLength(64)]
        public string? Department { get; set; }

        // Notification preferences
        public bool RemindEmployeeCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowEmployeePsyNotif { get; set; } = false;

        [Required]
        public string Frequency { get; set; } = "Daily";

        // Display only
        public string CompanyName { get; set; } = "";
    }
}
