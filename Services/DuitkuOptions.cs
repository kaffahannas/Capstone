namespace LightenUp.Web.Services;

// #Class DuitkuOptions#
public class DuitkuOptions
{
    public const string SectionName = "Duitku";

    public string MerchantCode { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string InquiryUrl { get; set; } = "https://sandbox.duitku.com/webapi/api/merchant/v2/inquiry";
    /// <summary>When true and credentials are empty, simulates payment without calling DuitKu.</summary>
    public bool UseMock { get; set; } = true;
}
