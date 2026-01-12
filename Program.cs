using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Database.SiteAnalysisNamespace;
using MonitoringServiceCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<MonitoringDbContext>();
builder.Services.AddSingleton<AuthorizeService>();
builder.Services.AddHttpClient<SiteDataDownloader>();
builder.Services.AddSingleton<NetWordAnalyzer>();

builder.Services.AddAuthentication("SimpleCookie")
    .AddCookie("SimpleCookie", options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.Name = "MonitoringServiceAuthCookie";
        options.AccessDeniedPath = "/LoginPage";
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication(); // Включаем аутентификацию
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();



using (MonitoringDbContext Monitiordb = new MonitoringDbContext())
{
    var usersLIst = new List<User>();
    Role adminRole = new Role { RoleName = "Admin"};
    Role clientRole = new Role { RoleName = "Client" };
    // создаем два объекта User

    User user1 = new User { Name = "Admin1", SecondName = "Admin", Password = "admin", UserRole = adminRole };
    User user2 = new User { Name = "Marat", SecondName = "Ismagilov", Password = "marat321", UserRole = clientRole };
    usersLIst.Add(user1);
    usersLIst.Add(user2);

    SiteAnalysis siteAnalysis = new SiteAnalysis {Url = "https://ru.wikipedia.org/wiki/%D0%93%D0%B0%D0%B4",DomainUrl= "https://ru.wikipedia.org/wiki" };

    // добавляем их в бд
    Monitiordb.Roles.AddRange(adminRole, clientRole);
    Monitiordb.Users.AddRange(user1, user2);
    Monitiordb.SiteAnalyses.AddRange(siteAnalysis);  
    Monitiordb.SaveChanges();
}
// получение данных
app.Run();
