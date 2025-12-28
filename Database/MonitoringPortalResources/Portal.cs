using MonitoringService.Database.MonitoringTasks;
using MonitoringServiceCore.Database.MonitoringPortalResources;

namespace MonitoringService.Database.MonitoringPortalResources
{
    public class Portal
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public PortalType Type { get; set; } // Government, Commercial, SocialMedia
       // public PortalStatus Status { get; set; } // Active, Inactive, Archived
        public DateTime CreatedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }

        // Настройки мониторинга
        public int CheckIntervalMinutes { get; set; }
        public bool IsMonitoringEnabled { get; set; }
        public string? ParserConfiguration { get; set; } // JSON конфиг

        public ICollection<MonitoringResource> Resources { get; set; }
        public ICollection<MonitoringTask> Tasks { get; set; }
    }
}
