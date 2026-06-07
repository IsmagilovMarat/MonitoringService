namespace MonitoringServiceCore.Database.BadWord
{
    public class DictionaryInfo
    {
        public int TotalWords { get; set; }
        public bool IsLoaded { get; set; }
        public string Source { get; set; } = string.Empty;
        public List<string> SampleWords { get; set; } = new();
        public bool MLEnabled { get; set; }
    }
}
