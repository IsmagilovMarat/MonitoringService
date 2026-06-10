namespace MonitoringServiceCore.Database.ExtremistMaterials
{
    public class ExtremistCheckResult
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; }
        public bool HasExtremistMaterials { get; set; }
        public List<FoundMaterial>? FoundMaterials { get; set; }
        public Guid FoundMaterialId { get; set; } 

        public string? ErrorMessage { get; set; }
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);
    }

}
