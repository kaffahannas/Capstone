using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // #Class ChangePasswordViewModel#
    public class ChangePasswordViewModel
    {
        public bool HasPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password Lama")]
        public string? OldPassword { get; set; }

        [Required(ErrorMessage = "Password baru wajib diisi.")]
        [StringLength(100, ErrorMessage = "Password minimal {2} karakter.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Password baru dan konfirmasi tidak cocok.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
