using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LightenUp.Web.Models.ViewModels
{
    public class HrProfileViewModel
    {
        // ── Personal ──────────────────────────────
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; }
        public bool IsApprovedByAdmin { get; set; }
        public DateTime? OnboardingCompletedAt { get; set; }

        // ── Employment ────────────────────────────
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }

        // ── Company ───────────────────────────────
        public string CompanyName { get; set; } = "";
        public string? CompanyAddress { get; set; }
        public string? CompanyRegistrationNumber { get; set; }
        public string? CompanyReferralCode { get; set; }
        public string? CompanyContactEmail { get; set; }
        public string? CompanyContactNumber { get; set; }
        public int DivisionCount { get; set; }
        public int ActiveEmployeeCount { get; set; }

        // ── Subscription ──────────────────────────
        public bool HasActiveSubscription { get; set; }
        public string? ActivePlanName { get; set; }
        public DateTime? ActiveUntil { get; set; }
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

        // Display only
        public string CompanyName { get; set; } = "";
    }
}
