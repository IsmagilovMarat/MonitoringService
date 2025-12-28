namespace MonitoringService.Database.ViolationsRules
{
    public enum ViolationStatus
    {
        New = 1,
        Confirmed = 2,
        FalsePositive = 3,
        Fixed = 4,
        InProgress = 5,
        Escalated = 6
    }
}
