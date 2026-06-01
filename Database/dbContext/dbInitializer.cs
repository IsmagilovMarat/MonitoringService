using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;

namespace MonitoringServiceCore.Database.dbContext
{
    public static class DbInitializer
    {
        public static void Initialize(MonitoringDbContext context)
        {
            if (!context.Roles.Any())
            {
                var roles = new[]
                {
                new Role { RoleName = "Admin" },
                new Role { RoleName = "Client" },
                new Role { RoleName = "Moderator" }
            };

                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            if (!context.Users.Any())
            {
                var adminRole = context.Roles.First(r => r.RoleName == "Admin");
                var clientRole = context.Roles.First(r => r.RoleName == "Client");

                var users = new[]
                {
        new User {
            Id = Guid.NewGuid(),
            Name = "Admin1",
            SecondName = "Admin",
            Email = "admin@monitoringservice.com",
            Password = "admin",
            UserRole = adminRole
        },
        new User {
            Id = Guid.NewGuid(),
            Name = "Marat",
            SecondName = "Ismagilov",
            Email = "marat@example.com",
            Password = "marat321",
            UserRole = clientRole
        }
    };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            if (!context.SiteAnalyses.Any())
            {
                context.SiteAnalyses.Add(new SiteAnalysis
                {
                    Url = "https://ru.wikipedia.org/wiki/%D0%93%D0%B0%D0%B4",
                    DomainUrl = "https://ru.wikipedia.org/wiki"
                });
                context.SaveChanges();
            }
        }
    }
}
