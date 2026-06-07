namespace MonitoringServiceCore.Database.BadWord
{
    public class WordContext
    {
        public string Word { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}
