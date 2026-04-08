
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.SystemConfigurations
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } 
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string? OldValues { get; set; } 
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }

        public User? User { get; set; }
    }
}
