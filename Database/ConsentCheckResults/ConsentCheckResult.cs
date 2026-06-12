namespace MonitoringServiceCore.Database.ConsentCheckResults
{
    public class ConsentCheckResult
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public DateTime CheckTime { get; set; }
        public bool HasConsentMechanism { get; set; }      
        public bool HasCheckboxConsent { get; set; }       
        public bool HasButtonConsent { get; set; }         
        public bool HasPrivacyPolicyLink { get; set; }     
        public string ConsentText { get; set; }           
        public List<string> FoundKeywords { get; set; }    
        public string ErrorMessage { get; set; }

        public bool IsCompliant => HasConsentMechanism && (HasCheckboxConsent || HasButtonConsent) && HasPrivacyPolicyLink;
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
    }
}
