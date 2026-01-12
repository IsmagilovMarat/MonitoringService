using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Pages;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using System.Reflection.Emit;

namespace MonitoringServiceCore.Database.dbContext
{
    public class MonitoringDbContext : DbContext
    {
         static int count = 0;
        public MonitoringDbContext()
        {
            if (count < 1)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
            
            count = count+1;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=diplomDb;Username=postgres;Password=postgres");
        }
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.Entity<User>()
                  .HasOne(u => u.UserRole)
                  .WithMany(r=>r.UsersList)
                  .HasForeignKey(u => u.RoleId);

            modelbuilder.Entity<SiteAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DomainUrl).HasMaxLength(450);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AnalyzedDate).IsRequired();

                
            });

        }
        //пользователи и безопасность
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SiteAnalysis> SiteAnalyses { get; set; }
        //public DbSet<Permission> Permissions { get; set; }
        //public DbSet<UserRole> UserRoles { get; set; }
        //public DbSet<RolePermission> RolePermissions { get; set; }

        // Мониторинг


        //public DbSet<Portal> Portals { get; set; }
        //public DbSet<MonitoringResource> Resources { get; set; }
        //public DbSet<ContentSnapshot> Snapshots { get; set; }
        //public DbSet<ContentAnalysisResult> AnalysisResults { get; set; }

        //// Нарушения
        //public DbSet<LegalRequirement> LegalRequirements { get; set; }
        //public DbSet<DetectionRule> DetectionRules { get; set; }
        //public DbSet<DetectedViolation> Violations { get; set; }

        //// Задачи
        //public DbSet<MonitoringTask> Tasks { get; set; }
        //public DbSet<TaskLog> TaskLogs { get; set; }

        //// Уведомления и отчеты
        //public DbSet<Notification> Notifications { get; set; }
        //public DbSet<Report> Reports { get; set; }

        //// Система
        //public DbSet<SystemConfiguration> Configurations { get; set; }
        //public DbSet<AuditLog> AuditLogs { get; set; }


    }

}
