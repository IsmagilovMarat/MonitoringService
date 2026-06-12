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
        public User? GetUserByEmail(string email, string password)
        {
            var user = _dbContext.Users
                .Include(x => x.UserRole)
                .FirstOrDefault(x => x.Email == email && x.Password == password);

            return user;
        }
    }
}