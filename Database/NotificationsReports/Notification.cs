using MonitoringService.Database.MonitoringTasks;
using MonitoringService.Database.ViolationsRules;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.NotificationsReports
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ViolationId { get; set; }
        public Guid? TaskId { get; set; }

        public NotificationType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public User? User { get; set; }
        public DetectedViolation? Violation { get; set; }
        public MonitoringTask? Task { get; set; }
    }
}
