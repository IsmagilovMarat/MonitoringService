namespace MonitoringServiceCore.Database.GoogleForms
{
    public class DomainStatistics
    {
        public string Domain { get; set; }
        public int TotalPagesChecked { get; set; }
        public int PagesWithGoogleForms { get; set; }
        public int TotalFormsFound { get; set; }

        public double Percentage => TotalPagesChecked > 0 ?
            (PagesWithGoogleForms * 100.0 / TotalPagesChecked) : 0;
    }
}
