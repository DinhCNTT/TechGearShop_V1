using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}

namespace TechGearShop_V1.Services
{
    public class EmailSenderService : Interfaces.IEmailSenderService
    {
        private readonly IConfiguration _config;

        public EmailSenderService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var host = _config["EmailSettings:SmtpServer"];
            var port = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];
            var senderName = _config["EmailSettings:SenderName"] ?? "TechGear";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || username == "your_email@gmail.com")
            {
                return; // Bỏ qua nếu cấu hình chưa có (ngăn lỗi sập server)
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
