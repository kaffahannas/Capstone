using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    public class CreatePasswordViewModel
    {
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kata Sandi wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Kata Sandi minimal 6 karakter")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi Kata Sandi wajib diisi")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Kata sandi tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}