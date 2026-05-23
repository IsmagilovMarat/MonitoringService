namespace MonitoringServiceCore.Email.Interface
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
