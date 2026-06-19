namespace LightenUp.Web.Models
{
    // #Class ErrorViewModel#
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
