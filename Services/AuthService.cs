
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;

namespace MonitoringServiceCore.Services
{
    public class AuthService
    {
        private readonly MonitoringDbContext _db;

        public AuthService(MonitoringDbContext db)
        {
            _db = db;
        }

        // Простая проверка логина/пароля
        public User? CheckUser(string username, string password)
        {
            return _db.Users
                .FirstOrDefault(u => u.Name == username && u.Password == password);
        }
    }
}