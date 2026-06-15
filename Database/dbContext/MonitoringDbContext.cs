using Microsoft.EntityFrameworkCore;
using MonitoringServiceCore.Database.Email;
using MonitoringServiceCore.Database.ExtremistMaterials;
using MonitoringServiceCore.Database.GoogleForms;
using MonitoringServiceCore.Database.MonitoringPortalResources;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using MonitoringServiceCore.Pages;
using System.Reflection.Emit;

namespace MonitoringServiceCore.Database.dbContext
{
    public class MonitoringDbContext : DbContext
    {

        public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options):base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            modelbuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.SecondName).HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasOne(u => u.UserRole)
                  .WithMany(r => r.UsersList)
                  .HasForeignKey(u => u.RoleId);
            });

            modelbuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            modelbuilder.Entity<SiteAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DomainUrl).HasMaxLength(450);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AnalyzedDate).IsRequired();

            });
            modelbuilder.Entity<ExtremistMaterial>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Number);
                entity.Property(e => e.Description).HasMaxLength(1000);
            });
            modelbuilder.Entity<FoundMaterial>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(f => f.CheckResult)
                 .WithMany(r => r.FoundMaterials)
                 .HasForeignKey(f => f.CheckResultId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            modelbuilder.Entity<ExtremistCheckResult>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            modelbuilder.Entity<GoogleFormsDetectionResult>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            modelbuilder.Entity<ScheduledEmail>(entity =>
            entity.HasKey(e => e.Id));

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SiteAnalysis> SiteAnalyses { get; set; }
        public DbSet<ExtremistMaterial> ExtremistMaterials { get; set; }
        public DbSet<FoundMaterial> FoundMaterials { get; set; }
        public DbSet<ExtremistCheckResult> ExtremistCheckResults { get; set; }

        public DbSet<MonitoringResource> Resources { get; set; }
        public DbSet<GoogleFormsDetectionResult> GoogleFormsDetectionResults { get; set; }

        public DbSet<ScheduledEmail> ScheduledEmails { get; set; }

    }

}
