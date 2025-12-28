using MonitoringService.Database.ViolationsRules;
using MonitoringServiceCore.Database.MonitoringPortalResources;

namespace MonitoringService.Database.ContentSnapshots
{
    public class ContentSnapshot
    {
        public Guid Id { get; set; }
        public Guid ResourceId { get; set; }
        public string ContentHash { get; set; } // Для обнаружения изменений
        public string? RawContent { get; set; } // Полный HTML/текст
        public string? ExtractedText { get; set; } // Очищенный текст
        public string? Metadata { get; set; } // JSON с метаданными
        public DateTime CapturedAt { get; set; }
       // public SnapshotStatus Status { get; set; } // Pending, Processed, Error

        public MonitoringResource Resource { get; set; }
        public ICollection<ContentAnalysisResult> AnalysisResults { get; set; }
        public ICollection<DetectedViolation> DetectedViolations { get; set; }
    }
}
