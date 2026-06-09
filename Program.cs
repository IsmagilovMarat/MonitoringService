using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using MonitoringServiceCore.Email.Interface;
using MonitoringServiceCore.Email.Jobs;
using MonitoringServiceCore.Email.Services;
using MonitoringServiceCore.Email.Settings;
using MonitoringServiceCore.Services;
using Quartz;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var myConneciton = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<MonitoringDbContext>(opt => opt.UseNpgsql(myConneciton));

        builder.Services.AddRazorPages();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<AuthorizeService>();
        builder.Services.AddScoped<SiteDataDownloader>();
        builder.Services.AddScoped<BadWordAnalyzer>();
        builder.Services.AddScoped<GoogleFormsDetector>();
        builder.Services.AddScoped<PersonalDataConsentService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHostedService<ExtremistMaterialUpdateService>();
        builder.Services.AddScoped<ExtremistMaterialsParser>();
        builder.Services.AddScoped<ExtremistMaterialChecker>();
        builder.Services.AddScoped<JobFactory>();
        builder.Services.AddScoped<DataJob>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<BadWordAnalyzer>();

        builder.Services.AddAuthentication("SimpleCookie")
            .AddCookie("SimpleCookie", options =>
            {
                options.LoginPath = "/Login";
                options.Cookie.Name = "MonitoringServiceAuthCookie";
                options.AccessDeniedPath = "/LoginPage";
            });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var serviceProvider = scope.ServiceProvider;
            try
            {
                DataScheduler.Start(serviceProvider);
            }
            catch (Exception)
            {
                throw;
            }
        }
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            dbContext.Database.EnsureCreated();
            DbInitializer.Initialize(dbContext);
        }
        app.Run();
    }
   
}
//https://www.geeksforgeeks.org/websites-apps/how-to-add-a-link-to-a-google-form/
//https://ru.wikipedia.org/wiki/Гады
//https://minjust.gov.ru/ru/extremist-materials/