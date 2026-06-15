using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringServiceCore.Database.Email
{
    public class ScheduledEmail
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ResourceId { get; set; }

        [MaxLength(500)]
        public string ResourceUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ResourceName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string RecipientEmail { get; set; } = string.Empty;

        public DateTime ScheduledTime { get; set; }

        public bool IsSent { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CheckResultSnapshot { get; set; } = string.Empty;

        public int DelayDays { get; set; }
    }
}