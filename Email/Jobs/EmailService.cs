using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Email.Interface;
using MonitoringServiceCore.Services;
using Quartz;
using System.Net.Mail;

namespace MonitoringServiceCore.Email.Jobs
{
    [DisallowConcurrentExecution]
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailService(ILogger<EmailService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
       

        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var from = "maratismage@mail.ru";
            var pass = "u5vuG1NSMJ7A3PBXx5at";
            SmtpClient client = new SmtpClient("smtp.mail.ru", 587);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(from, pass);
            client.EnableSsl = true;
            var mail = new MailMessage(from, toEmail);
            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = true;
            return client.SendMailAsync(mail);

        }
    }
}
