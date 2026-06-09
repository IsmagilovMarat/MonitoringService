namespace MonitoringServiceCore.Database.ExtremistMaterials
{
    public class FoundMaterial
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public int Count { get; set; }
        public string? Description { get; set; }
        public string? MatchedKeyword { get; set; }
        public string? MatchType { get; set; }
        public string? Context { get; set; }
        public DateTime? DecisionDate { get; set; }
        public Guid CheckResultId { get; set; }
        public  ExtremistCheckResult? CheckResult { get; set; }
    }
}
