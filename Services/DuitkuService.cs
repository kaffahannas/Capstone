using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LightenUp.Web.Services;

public class DuitkuCreatePaymentRequest
{
    public string MerchantOrderId { get; set; } = "";
    public decimal Amount { get; set; }
    public string ProductDetails { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CallbackUrl { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
}

public class DuitkuCreatePaymentResult
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Reference { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsMock { get; set; }
}

public class DuitkuCallbackPayload
{
    public string? MerchantOrderId { get; set; }
    public string? Reference { get; set; }
    public string? ResultCode { get; set; }
    public string? ResultMessage { get; set; }
    public string? Amount { get; set; }
}

public class DuitkuService
{
    private readonly DuitkuOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DuitkuService> _log;

    public DuitkuService(
        Microsoft.Extensions.Options.IOptions<DuitkuOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<DuitkuService> log)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _log = log;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.MerchantCode) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<DuitkuCreatePaymentResult> CreatePaymentAsync(DuitkuCreatePaymentRequest request, CancellationToken ct = default)
    {
        if (_options.UseMock || !IsConfigured)
        {
            _log.LogInformation("DuitKu mock payment for order {OrderId}", request.MerchantOrderId);
            return new DuitkuCreatePaymentResult
            {
                Success = true,
                IsMock = true,
                Reference = "MOCK-" + request.MerchantOrderId,
                PaymentUrl = request.ReturnUrl + (request.ReturnUrl.Contains('?') ? "&" : "?") + "mock=1&orderId=" + Uri.EscapeDataString(request.MerchantOrderId)
            };
        }

        var amount = (int)Math.Round(request.Amount, 0);
        var signature = ComputeSha256($"{_options.MerchantCode}{request.MerchantOrderId}{amount}{_options.ApiKey}");

        var body = new
        {
            merchantCode = _options.MerchantCode,
            paymentAmount = amount,
            merchantOrderId = request.MerchantOrderId,
            productDetails = request.ProductDetails,
            email = request.CustomerEmail,
            customerVaName = request.CustomerName,
            callbackUrl = request.CallbackUrl,
            returnUrl = request.ReturnUrl,
            signature,
            expiryPeriod = 60
        };

        try
        {
            var client = _httpClientFactory.CreateClient("Duitku");
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_options.InquiryUrl, content, ct);
            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("DuitKu HTTP {Status}: {Body}", response.StatusCode, responseText);
                return new DuitkuCreatePaymentResult { Success = false, ErrorMessage = "Payment gateway unavailable." };
            }

            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            var statusCode = root.TryGetProperty("statusCode", out var sc) ? sc.GetString() : null;
            if (statusCode != "00")
            {
                var msg = root.TryGetProperty("statusMessage", out var sm) ? sm.GetString() : "Unknown error";
                return new DuitkuCreatePaymentResult { Success = false, ErrorMessage = msg };
            }

            var paymentUrl = root.TryGetProperty("paymentUrl", out var pu) ? pu.GetString() : null;
            var reference = root.TryGetProperty("reference", out var rf) ? rf.GetString() : null;
            return new DuitkuCreatePaymentResult
            {
                Success = true,
                PaymentUrl = paymentUrl,
                Reference = reference
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "DuitKu create payment failed");
            return new DuitkuCreatePaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public bool VerifyCallbackSignature(string merchantCode, string amount, string merchantOrderId, string signature)
    {
        if (!IsConfigured) return true;
        var expected = ComputeMd5($"{merchantCode}{amount}{merchantOrderId}{_options.ApiKey}");
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeMd5(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
