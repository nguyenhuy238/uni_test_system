using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _opt;

        public SmtpEmailSender(IOptions<EmailOptions> opt)
        {
            _opt = opt.Value;
        }

        public async Task SendAsync(string email, string subject, string htmlMessage)
        {
            using var client = new SmtpClient(_opt.Host, _opt.Port)
            {
                Credentials = new NetworkCredential(_opt.User, _opt.Pass),
                EnableSsl = _opt.EnableSsl
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_opt.From, _opt.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(email);

            await client.SendMailAsync(mail);
        }
    }
}
