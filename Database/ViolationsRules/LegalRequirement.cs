namespace MonitoringService.Database.ViolationsRules
{
    public class LegalRequirement
    {
        public Guid Id { get; set; }
        public string Code { get; set; } // Нормативный код (ФЗ-123 ст. 45)
        public string Title { get; set; }
        public string Description { get; set; }
        public string? LegalDocument { get; set; } // Название документа
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsActive { get; set; }

        public ICollection<DetectionRule> DetectionRules { get; set; }
    }
}
