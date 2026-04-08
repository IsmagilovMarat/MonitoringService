namespace MonitoringServiceCore.Database.GoogleForms
{
    public class SecurityAnalysis
    {
        public string SecurityLevel { get; set; }
        public bool HasHttps { get; set; }
        public bool HasCSP { get; set; }
        public bool HasMixedContent { get; set; }
        public int ExternalScriptsCount { get; set; }
    }
}
