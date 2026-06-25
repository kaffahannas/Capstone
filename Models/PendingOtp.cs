namespace LightenUp.Web.Models;

public class PendingOtp
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string OtpCode { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
