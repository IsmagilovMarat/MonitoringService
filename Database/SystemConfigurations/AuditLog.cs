
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.SystemConfigurations
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } // Created, Updated, Deleted
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }

        public User? User { get; set; }
    }
}
