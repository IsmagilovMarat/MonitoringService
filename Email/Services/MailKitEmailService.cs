using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MonitoringServiceCore.Email.Interface;
using MonitoringServiceCore.Email.Settings;

namespace MonitoringServiceCore.Email.Services
{
    public class MailKitEmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IOptions<SmtpSettings> smtpSettings, ILogger<MailKitEmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                // Формирование письма
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart("html") { Text = message }; // Письмо в формате HTML

                // Отправка письма
                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port,
                    _smtpSettings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Письмо для {toEmail} успешно отправлено.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке письма для {toEmail}");
                throw;
            }
        }
    }
}
