using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // #Class ForgotPasswordViewModel#
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;
    }
}
