using MonitoringServiceCore.Services;

namespace MonitoringServiceCore.Database.BadWord
{
    public class BadWordsAnalysis
    {
        public Dictionary<string, int> FoundWords { get; set; } = new();
        public int TotalCount { get; set; }
        public List<WordContext> WordsWithContext { get; set; } = new();
        public List<WordPosition> WordPositions { get; set; } = new();
        public bool HasBadWords => TotalCount > 0;
    }
}
