using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var MerchantCode = "DS31885";
var ApiKey = "a75277b2b3b89fa4eabb1104568b3f42";
var MerchantOrderId = "LU-TEST-" + DateTime.Now.Ticks;
var amount = 10000;
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

var headerSignatureInput = $"{MerchantCode}{timestamp}{ApiKey}";
var headerHash = SHA256.HashData(Encoding.UTF8.GetBytes(headerSignatureInput));
var headerSignature = Convert.ToHexString(headerHash).ToLowerInvariant();

var bodySignatureInput = $"{MerchantCode}{MerchantOrderId}{amount}{ApiKey}";
var bodyHash = SHA256.HashData(Encoding.UTF8.GetBytes(bodySignatureInput));
var bodySignature = Convert.ToHexString(bodyHash).ToLowerInvariant();

var body = new
{
    paymentAmount = amount,
    merchantOrderId = MerchantOrderId,
    productDetails = "Test Payment",
    email = "test@example.com",
    customerVaName = "Kaffah",
    callbackUrl = "https://example.com/dk/cb",
    returnUrl = "https://example.com/return",
    signature = bodySignature,
    expiryPeriod = 60
};

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("x-duitku-merchantcode", MerchantCode);
client.DefaultRequestHeaders.Add("x-duitku-timestamp", timestamp);
client.DefaultRequestHeaders.Add("x-duitku-signature", headerSignature);

var json = JsonSerializer.Serialize(body);
using var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync("https://api-sandbox.duitku.com/api/merchant/createInvoice", content);
var responseText = await response.Content.ReadAsStringAsync();

Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine($"Body: {responseText}");
