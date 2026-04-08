namespace MonitoringServiceCore.Database.GoogleForms
{
    public class FormDetail
    {
        public int Index { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }
        public int InputFieldsCount { get; set; }
        public bool HasSubmitButton { get; set; }
    }
}
