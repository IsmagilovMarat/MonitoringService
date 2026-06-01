using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringServiceCore.Services
{
    public class AuthorizeService
    {
        private MonitoringDbContext _dbContext; 
        public AuthorizeService(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public User GetUserFromDb (string name, string password)
        {
            var user = _dbContext.Users.
                Include(x => x.UserRole).
                FirstOrDefault(x => x.Name == name && x.Password == password);
               
            if (user != null)
            {
                return user;
            }
            else
            {
                return null;
            }
        }
        public bool CreateUser(string username, string secondName, string password, string roleName = "Client")
        {
            var role = _dbContext.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null) return false;

            var user = new User
            {
                Name = username,
                SecondName = secondName ?? string.Empty,
                Password = password, // В реальном проекте хешируйте пароль!
                UserRole = role,
                RoleId = role.Id
            };

            _dbContext.Users.Add(user);
            return _dbContext.SaveChanges() > 0;
        }
    }
}
