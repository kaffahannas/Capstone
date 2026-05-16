using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── HR site register form (no role picker) ───
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

    public class HrOnboardingProgress
    {
        public int Current { get; set; }
        public int Total => 3;
        public int Percent => (int)((double)Current / Total * 100);
    }

    // Step 1: Photo
    public class HrOnboardingPhotoViewModel
    {
        [Required(ErrorMessage = "Silakan unggah foto diri.")]
        public IFormFile? Photo { get; set; }
    }

    // Step 2: Academic
    public class HrOnboardingAcademicViewModel
    {
        [Required(ErrorMessage = "Gelar terakhir wajib dipilih.")]
        public string LastDegree { get; set; } = string.Empty;

        [Required(ErrorMessage = "Universitas asal wajib diisi.")]
        public string University { get; set; } = string.Empty;

        [Required(ErrorMessage = "Silakan unggah dokumen pendukung.")]
        public IFormFile? AcademicDocument { get; set; }
    }

    // Step 3: Company — either create new or join existing
    public class HrOnboardingCompanyViewModel
    {
        [Required(ErrorMessage = "Pilih salah satu opsi.")]
        public string Mode { get; set; } = "Create";  // "Create" or "Join"

        // For Create
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? RegistrationNumber { get; set; }
        public IFormFile? SupportDocument { get; set; }

        // For Join
        public string? ReferralCode { get; set; }

        // For both
        [Required(ErrorMessage = "Divisi wajib diisi.")]
        public string Department { get; set; } = string.Empty;
    }
}
