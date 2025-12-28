namespace MonitoringServiceCore.Database.Roles
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }    
        public string? SecondName { get; set; }
        public string Password { get; set; }
        public Role UserRole { get; set; }
        public Guid RoleId { get; set; }
    }
}
