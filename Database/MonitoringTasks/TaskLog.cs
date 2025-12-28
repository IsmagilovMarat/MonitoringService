using MonitoringService.Database.MonitoringPortalResources;
using MonitoringServiceCore.Database.MonitoringPortalResources;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.MonitoringTasks
{
    public class TaskLog
    {
        public Guid Id { get; set; }
        public Guid PortalId { get; set; }
        public Guid? ResourceId { get; set; } // Если null - все ресурсы портала
        public Guid CreatedByUserId { get; set; }

        public TaskType Type { get; set; } // Scheduled, Manual, OneTime
        public TaskStatus Status { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Результаты
        public int TotalResources { get; set; }
        public int ProcessedResources { get; set; }
        public int FailedResources { get; set; }
        public int ViolationsFound { get; set; }
        public string? ErrorLog { get; set; }

        public Portal Portal { get; set; }
        public MonitoringResource? Resource { get; set; }
        public User CreatedByUser { get; set; }
        public ICollection<TaskLog> Logs { get; set; }
    }
}
