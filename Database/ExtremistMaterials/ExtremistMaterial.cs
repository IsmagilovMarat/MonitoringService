namespace MonitoringServiceCore.Database.ExtremistMaterials
{
    public class ExtremistMaterial
    {
        public Guid Id { get; set; }
        public int Number { get; set; }            
        public string? Text { get; set; }   
        public string Description { get; set; }   
        public DateTime? DecisionDate { get; set; }
        public string RawText { get; set; }       
        public int PageNumber { get; set; }       
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
