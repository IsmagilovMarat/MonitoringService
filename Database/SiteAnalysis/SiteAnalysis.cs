namespace MonitoringServiceCore.Database.SiteAnalysisNamespace
{
    public class SiteAnalysis
    {
        public Guid Id { get; set; }

        public string DomainUrl { get; set; } 
        public string Url { get; set; } = string.Empty;
        public DateTime AnalyzedDate { get; set; }
        public int CountOfViolations { get; set; } = 0;
       
    }
}
