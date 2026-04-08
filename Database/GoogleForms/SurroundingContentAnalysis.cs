namespace MonitoringServiceCore.Database.GoogleForms
{
    public class SurroundingContentAnalysis
    {
        public bool ContainsProfanity { get; set; }
        public int ProfanityCount { get; set; }
        public List<string> ProfanityExamples { get; set; } = new List<string>();
        public bool ContainsSensitiveFields { get; set; }
    }
}
