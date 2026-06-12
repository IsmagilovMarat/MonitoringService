using MonitoringServiceCore.Services;

namespace MonitoringServiceCore.Database.BadWord
{
    public class AnalysisResult
    {
        public int TotalCharacters { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, int> BadWordsFound { get; set; } = new();
        public int TotalBadWordsCount { get; set; }
        public List<WordContext> BadWordsWithContext { get; set; } = new();
        public List<WordPosition> WordPositions { get; set; } = new();
        public bool HasBadWords => TotalBadWordsCount > 0;
        
    }
}
