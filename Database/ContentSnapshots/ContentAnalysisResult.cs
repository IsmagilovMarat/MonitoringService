namespace MonitoringService.Database.ContentSnapshots
{
    public class ContentAnalysisResult
    {
        public Guid Id { get; set; }
        public Guid SnapshotId { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public string ResultData { get; set; } // JSON с результатами
        public double? ConfidenceScore { get; set; }
        public DateTime AnalyzedAt { get; set; }

        public ContentSnapshot Snapshot { get; set; }
    }
}
