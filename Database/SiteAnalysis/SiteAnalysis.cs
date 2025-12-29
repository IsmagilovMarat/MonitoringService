namespace MonitoringServiceCore.Database.SiteAnalysisNamespace
{
    public class SiteAnalysis
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime AnalyzedDate { get; set; }
        public int NetCount { get; set; }
        public int DotNetCount { get; set; }
        public int AspNetCount { get; set; }
        public int TotalCharacters { get; set; }
        public string? Content { get; set; }
    }
}
