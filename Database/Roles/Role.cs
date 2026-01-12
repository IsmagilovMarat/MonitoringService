namespace MonitoringServiceCore.Database.Roles
{
    public class Role
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public List<User> UsersList { get; set; } = new();
        public bool isAdmin { get; set; }
            
        
    }
}
