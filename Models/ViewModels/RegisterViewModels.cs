using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    public class PublicRegisterViewModel
    {
        [Required(ErrorMessage = "Nama Lengkap wajib diisi")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pilih jenis akun Anda")]
        public string AccountType { get; set; } = string.Empty;
    }

    public class HrRegisterViewModel
    {
        [Required(ErrorMessage = "Nama HR wajib diisi")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Instansi wajib diisi")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kode rahasia wajib diisi")]
        public string SecretCode { get; set; } = string.Empty;
    }
}