using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // ViewModel untuk Langkah 1: Foto Diri
    public class OnboardingStep1ViewModel
    {
        [Required(ErrorMessage = "Silakan unggah foto diri Anda.")]
        public IFormFile? ProfilePhoto { get; set; }
    }

    // ViewModel untuk Langkah 2: Status Akademik
    public class OnboardingStep2ViewModel
    {
        [Required(ErrorMessage = "Gelar terakhir wajib dipilih.")]
        public string LastDegree { get; set; } = string.Empty;

        [Required(ErrorMessage = "Universitas asal wajib diisi.")]
        public string University { get; set; } = string.Empty;

        [Required(ErrorMessage = "Silakan unggah dokumen pendukung (Ijazah/Transkrip).")]
        public IFormFile? AcademicDocument { get; set; }
    }

    // ViewModel untuk Langkah 3: Status Praktek
    public class OnboardingStep3ViewModel
    {
        [Required(ErrorMessage = "Lokasi praktek wajib diisi.")]
        public string PracticeLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nomor SIAP HIMPSI wajib diisi.")]
        public string SiapNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nomor SIPP wajib diisi.")]
        public string SippNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Silakan unggah scan dokumen pendukung (STR/SIPP).")]
        public IFormFile? StrDocument { get; set; }
    }
}