
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.NotificationsReports
{
    public class Report
    {
        public Guid Id { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Name { get; set; }
        public ReportType Type { get; set; }
        public string Parameters { get; set; } // JSON параметров
        public DateTime CreatedAt { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public string? FilePath { get; set; }
        public string? DownloadUrl { get; set; }

        public User CreatedByUser { get; set; }
    }
}
