using MonitoringServiceCore.Services;

namespace MonitoringServiceCore.Database.BadWord
{
    public class MLDetectionResult
    {
        public bool HasProfanity { get; set; }
        public float Probability { get; set; }
        public Dictionary<string, int> FoundWords { get; set; } = new();
        public List<WordPosition> WordPositions { get; set; } = new();
        public bool IsMLBased { get; set; }
    }
}
