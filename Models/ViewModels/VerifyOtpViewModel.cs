using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // #Class VerifyOtpViewModel#
    public class VerifyOtpViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kode OTP tidak boleh kosong")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Kode OTP harus diisi lengkap 4 digit")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
