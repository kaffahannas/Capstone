using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── HR site register form (no role picker) ───
    // #Class HrRegisterFormViewModel#
    public class HrRegisterFormViewModel
    {
        [Required(ErrorMessage = "Nama HR wajib diisi")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;
    }

    // ─── HR onboarding 3-step wizard ───

    // #Class HrOnboardingProgress#
    public class HrOnboardingProgress
    {
        public int Current { get; set; }
        public int Total => 3;
        public int Percent => (int)((double)Current / Total * 100);
    }

    // Step 1: Photo
    // #Class HrOnboardingPhotoViewModel#
    public class HrOnboardingPhotoViewModel
    {
        public IFormFile? Photo { get; set; }
        public bool HasExistingPhoto { get; set; }
    }

    // Step 2: Academic
    // #Class HrOnboardingAcademicViewModel#
    public class HrOnboardingAcademicViewModel
    {
        [Required(ErrorMessage = "Gelar terakhir wajib dipilih.")]
        public string LastDegree { get; set; } = string.Empty;

        [Required(ErrorMessage = "Universitas asal wajib diisi.")]
        public string University { get; set; } = string.Empty;

        public IFormFile? AcademicDocument { get; set; }
        public bool HasExistingDocument { get; set; }
    }

    // Step 3: Register company (referral code is created after subscription payment)
    // #Class HrOnboardingCompanyViewModel#
    public class HrOnboardingCompanyViewModel
    {
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? RegistrationNumber { get; set; }
        public IFormFile? SupportDocument { get; set; }
        public bool HasExistingSupportDocument { get; set; }

        [Required(ErrorMessage = "Divisi wajib diisi.")]
        public string Department { get; set; } = string.Empty;
    }
}
