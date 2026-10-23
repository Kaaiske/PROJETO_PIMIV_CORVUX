using System.Threading.Tasks;
using CorvusIAM.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace CorvusIAM.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
            var portStr = _configuration["Smtp:Port"] ?? "587";
            int.TryParse(portStr, out var port);
            var email = _configuration["Smtp:Email"];
            var password = _configuration["Smtp:Password"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP não configurado; e-mail para {Recipient} não foi enviado.", to);
                throw new InvalidOperationException("SMTP não configurado.");
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(email));
            mimeMessage.To.Add(MailboxAddress.Parse(to));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            // Connect using STARTTLS
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(email, password);
            await smtp.SendAsync(mimeMessage);
            await smtp.DisconnectAsync(true);
        }
    }
}
