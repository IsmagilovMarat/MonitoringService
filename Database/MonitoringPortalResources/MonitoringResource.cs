using MonitoringService.Database.ContentSnapshots;
using MonitoringService.Database.MonitoringPortalResources;

namespace MonitoringServiceCore.Database.MonitoringPortalResources
{
    public class MonitoringResource
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public ResourceType Type { get; set; } 
        public PortalType TypePortal { get; set; }
        public bool IsActive { get; set; }
    }
}
