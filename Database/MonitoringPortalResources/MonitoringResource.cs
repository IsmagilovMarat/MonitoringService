using MonitoringService.Database.ContentSnapshots;
using MonitoringService.Database.MonitoringPortalResources;

namespace MonitoringServiceCore.Database.MonitoringPortalResources
{
    public class MonitoringResource
    {
        public Guid Id { get; set; }
        public Guid PortalId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public ResourceType Type { get; set; } 
        public string? SelectorPath { get; set; }
        public string? ParserConfiguration { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }

        public Portal Portal { get; set; }
        public ICollection<ContentSnapshot> Snapshots { get; set; }
    }
}
