using MonitoringServiceCore.Email.Interface;
using Quartz;

namespace MonitoringServiceCore.Email.Jobs
{
    public class DataJob : IJob
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public DataJob(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var emailsender = scope.ServiceProvider.GetService<IEmailService>();
                try
                {
                  //  await emailsender.SendEmailAsync("fullstack_web_developer@mail.ru", "example", "hello");

                }
                catch
                {

                }
            }
        }
    }
}
