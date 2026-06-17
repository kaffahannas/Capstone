using LightenUp.Web.Data;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LightenUp.Web.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
[Route("dk")]
public class PaymentWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly DuitkuService _duitku;
    private readonly ILogger<PaymentWebhookController> _log;

    public PaymentWebhookController(ApplicationDbContext context, DuitkuService duitku, ILogger<PaymentWebhookController> log)
    {
        _context = context;
        _duitku = duitku;
        _log = log;
    }

    [HttpPost("cb")]
    public async Task<IActionResult> Callback([FromForm] DuitkuCallbackForm form)
    {
        var payloadJson = JsonSerializer.Serialize(form);
        _log.LogInformation("DuitKu callback: {Payload}", payloadJson);

        if (string.IsNullOrEmpty(form.MerchantOrderId))
            return BadRequest();

        var payment = await _context.PaymentTransactions
            .Include(p => p.Subscription)
            .Include(p => p.CompanySubscription)
            .FirstOrDefaultAsync(p => p.MerchantOrderId == form.MerchantOrderId);

        if (payment == null)
            return NotFound();

        // Idempotency: skip if already processed
        if (payment.PaymentStatus == "paid")
        {
            _log.LogInformation("DuitKu callback for already-paid order {OrderId}, skipping.", form.MerchantOrderId);
            return Ok();
        }

        if (string.IsNullOrEmpty(form.Signature))
        {
            _log.LogWarning("Missing DuitKu signature for {OrderId}", form.MerchantOrderId);
            return Unauthorized();
        }

        var amountStr = ((int)payment.Amount).ToString();
        if (!_duitku.VerifyCallbackSignature(
                form.MerchantCode ?? "",
                amountStr,
                form.MerchantOrderId,
                form.Signature))
        {
            _log.LogWarning("Invalid DuitKu signature for {OrderId}", form.MerchantOrderId);
            return Unauthorized();
        }

        payment.CallbackPayload = payloadJson;
        payment.ResultCode = form.ResultCode;
        payment.ResultMessage = form.ResultMessage;
        payment.DuitkuReference = form.Reference ?? payment.DuitkuReference;

        if (form.ResultCode == "00")
        {
            await PaymentCompletionService.MarkPaidAsync(_context, payment);
        }
        else
        {
            payment.PaymentStatus = "failed";
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    public class DuitkuCallbackForm
    {
        public string? MerchantCode { get; set; }
        public string? Amount { get; set; }
        public string? MerchantOrderId { get; set; }
        public string? Reference { get; set; }
        public string? ResultCode { get; set; }
        public string? ResultMessage { get; set; }
        public string? Signature { get; set; }
    }
}
