using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Jerely_api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendContactEmailAsync(string fromEmail, string fromName, string message)
        {
            var section = _config.GetSection("EmailSettings");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(MailboxAddress.Parse(section["AdminEmail"]));
            email.Subject = $"Confirmation du compte Jerely - {fromName}";
            email.Body = new TextPart("plain") { Text = message };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(section["SmtpHost"], int.Parse(section["SmtpPort"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(section["SmtpUser"], section["SmtpPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
