using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Email.Interface;
using Quartz;
using System.Text;

namespace MonitoringServiceCore.Email.Jobs
{
    [DisallowConcurrentExecution]
    public class DataJob : IJob
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public DataJob(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<MonitoringDbContext>();    
            var emailService= scope.ServiceProvider.GetService<IEmailService>();
            try
            {
                var now = DateTime.Now;
                var emailsToSend = await dbContext.ScheduledEmails
                    .Where(e => !e.IsSent && e.ScheduledTime <= now)
                    .ToListAsync();

                foreach (var email in emailsToSend)
                {
                    try
                    {
                        var subject = $"Повторный отчёт: {email.ResourceName} - через {email.DelayDays} день после проверки";
                        var messageBuilder = new StringBuilder();
                        messageBuilder.AppendLine($"<h2>Повторный отчёт по ресурсу \"{email.ResourceName}\"</h2>");
                        messageBuilder.AppendLine($"<p>URL:<a href='{email.ResourceUrl}'>{email.ResourceUrl}</a></p>");
                        messageBuilder.AppendLine($"<p>Дата проверки:> {email.CreatedAt:dd.MM.yyyy HH:mm}</p>");
                        messageBuilder.AppendLine($"<p>это письмо отправлено через {email.DelayDays} дней после проверки.</p>");
                        messageBuilder.AppendLine("<hr/>");
                        messageBuilder.AppendLine("<h3>Результаты проверки:</h3>");

                        messageBuilder.AppendLine($"<p>Статус: {(email.CheckResultSnapshot.Contains("HasViolations\":true") ? " Есть нарушения" : "Нарушений нет")}</p>");

                        await emailService.SendEmailAsync(email.RecipientEmail, subject, messageBuilder.ToString());

                        email.IsSent = true;
                        await dbContext.SaveChangesAsync();

                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
