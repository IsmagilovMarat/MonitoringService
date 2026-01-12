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
    }
}
