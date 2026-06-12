namespace MonitoringServiceCore.Database.SiteAnalysisNamespace
{
    public class SiteAnalysis
    {
        public Guid Id { get; set; }
        public string DomainUrl { get; set; } 
        public string Url { get; set; } = string.Empty;
        public DateTime AnalyzedDate { get; set; }
        public int CountOfViolations { get; set; } = 0;
        public bool HasGoogleForms { get; set; }
        public bool HasBadWords { get; set; }
        public bool HasExtimistMaterial { get; set; }
        public bool HasHotPersonalDataPermissons { get; set; }
        public List<string>? GoogleFormsFound { get; set; }
        public int OverallScore { get; set; }
        public bool HasPrivacyPolicy { get; set; }
        public bool HasConsent { get; set; }
        public List<string>? ExtremismFound { get; set; }
        public int TotalBadWordsCount { get; set; }

    }
}
