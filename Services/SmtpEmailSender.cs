using System.Net;
using System.Net.Mail;

namespace LightenUp.Web.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toAddress, string subject, string body, bool isHtml = false);
    }

    public class SmtpOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FromAddress { get; set; }
        public string? FromName { get; set; }
        public bool EnableSsl { get; set; } = true;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(FromAddress);
    }

    // #Class SmtpEmailSender#
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opts;
        private readonly ILogger<SmtpEmailSender> _log;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> log)
        {
            _log = log;
            _opts = new SmtpOptions
            {
                Host = config["Email:Smtp:Host"],
                Port = int.TryParse(config["Email:Smtp:Port"], out var p) ? p : 587,
                Username = config["Email:Smtp:Username"],
                Password = config["Email:Smtp:Password"],
                FromAddress = config["Email:FromAddress"],
                FromName = config["Email:FromName"] ?? "LightenUp",
                EnableSsl = config["Email:Smtp:EnableSsl"]?.ToLowerInvariant() != "false"
            };
        }

        // #Function SendAsync#
        public async Task SendAsync(string toAddress, string subject, string body, bool isHtml = false)
        {
            if (!_opts.IsConfigured)
                throw new InvalidOperationException(
                    "SMTP not configured. Set Email:Smtp:Host/Port/Username/Password and Email:FromAddress " +
                    "via 'dotnet user-secrets set' before using this feature.");

            using var msg = new MailMessage
            {
                From = new MailAddress(_opts.FromAddress!, _opts.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            msg.To.Add(new MailAddress(toAddress));

            using var client = new SmtpClient(_opts.Host!, _opts.Port)
            {
                Credentials = new NetworkCredential(_opts.Username, _opts.Password),
                EnableSsl = _opts.EnableSsl
            };

            try
            {
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to send email to {To}", toAddress);
                throw;
            }
        }
    }
}
