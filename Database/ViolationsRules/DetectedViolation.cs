using MonitoringService.Database.ContentSnapshots;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringService.Database.ViolationsRules
{
    public class DetectedViolation
    {
        public Guid Id { get; set; }
        public Guid SnapshotId { get; set; }
        public Guid RuleId { get; set; }
        public Guid? RequirementId { get; set; }

        // Контекст нарушения
        public string ViolationText { get; set; } // Текст с нарушением
        public string? Context { get; set; } // Окружающий текст
        public string? XPath { get; set; } // Позиция на странице
        public int? StartPosition { get; set; }
        public int? EndPosition { get; set; }

        // Статус и метаданные
        public ViolationStatus Status { get; set; } // New, Confirmed, FalsePositive, Fixed
        public SeverityLevel Severity { get; set; }
        public double Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
        public Guid? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? Comment { get; set; }

        // Связи
        public ContentSnapshot Snapshot { get; set; }
        public DetectionRule Rule { get; set; }
        public LegalRequirement? Requirement { get; set; }
        public User? ConfirmedByUser { get; set; }
    }
}
