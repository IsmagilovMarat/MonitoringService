using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringServiceCore.Services
{
    public class AuthorizeService
    {
        private readonly MonitoringDbContext _dbContext;

        public AuthorizeService(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public User? GetUserFromDb(string name, string password)
        {
            var user = _dbContext.Users
                .Include(x => x.UserRole)
                .FirstOrDefault(x => x.Name == name && x.Password == password);

            return user;
        }
        public User? GetUserByEmail(string email, string password)
        {
            var user = _dbContext.Users
                .Include(x => x.UserRole)
                .FirstOrDefault(x => x.Email == email && x.Password == password);

            return user;
        }
        public User? GetUserByEmailOnly(string email)
        {
            var user = _dbContext.Users
                .Include(x => x.UserRole)
                .FirstOrDefault(x => x.Email == email);

            return user;
        }

        public bool CreateUser(string username, string secondName, string email, string password, string roleName = "Client")
        {
            var role = _dbContext.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null) return false;

            var existingUser = _dbContext.Users.FirstOrDefault(u => u.Name == username || u.Email == email);
            if (existingUser != null) return false;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = username,
                SecondName = secondName ?? string.Empty,
                Email = email,
                Password = password,
                UserRole = role,
                RoleId = role.Id
            };

            _dbContext.Users.Add(user);
            return _dbContext.SaveChanges() > 0;
        }
    }
}