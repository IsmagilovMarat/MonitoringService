namespace MonitoringService.Database.ViolationsRules
{
    public class DetectionRule
    {
        public Guid Id { get; set; }
        public Guid RequirementId { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; } // Regex/JSON конфигурация
        public string? MlModelPath { get; set; }
        public string? Keywords { get; set; } // Слова через запятую
        public SeverityLevel Severity { get; set; }
        public bool IsActive { get; set; }
        public double ConfidenceThreshold { get; set; } = 0.8;

        public LegalRequirement Requirement { get; set; }
        public ICollection<DetectedViolation> DetectedViolations { get; set; }
    }
}
