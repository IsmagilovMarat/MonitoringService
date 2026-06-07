namespace MonitoringServiceCore.Database.BadWord
{
    public class WordPosition
    {
        public string Word { get; set; } = string.Empty;
        public int Position { get; set; }
        public int LineNumber { get; set; }
        public string Context { get; set; } = string.Empty;
    }

}
