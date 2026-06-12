using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace MonitoringServiceCore.Email.Jobs
{
    public static class DataScheduler
    {

        public static async void Start(IServiceProvider serviceProvider)
        {
            IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = serviceProvider.GetService<JobFactory>();
            await scheduler.Start();

            IJobDetail jobDetail = JobBuilder.Create<DataJob>().Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("MailingTrigger", "default")
                .StartNow()
                .WithSimpleSchedule(x => x
                .WithIntervalInHours(1)
                .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
        }
    }
}
