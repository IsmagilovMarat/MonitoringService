using MonitoringServiceCore.Services;

namespace MonitoringServiceCore.Database.GoogleForms
{
    public class GoogleFormsDetectionResult
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public DateTime DetectionTime { get; set; }
        public bool HasGoogleForms { get; set; }
        public bool HtmlLoaded { get; set; }
        public int HtmlLength { get; set; }
        public bool IsPotentiallyMalicious { get; set; }
        public string ErrorMessage { get; set; }
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
           
    }
}
