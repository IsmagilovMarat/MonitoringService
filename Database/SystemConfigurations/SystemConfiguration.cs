using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.SystemConfigurations
{
    public class SystemConfiguration
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string? Description { get; set; }
        public string DataType { get; set; } // String, Int, Bool, DateTime
        public bool IsEncrypted { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedByUserId { get; set; }

        public User UpdatedByUser { get; set; }
    }
}
