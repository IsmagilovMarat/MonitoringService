using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<MonitoringDbContext>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddHttpClient<SiteDataDownloader>();
builder.Services.AddSingleton<NetWordAnalyzer>();

builder.Services.AddAuthentication("SimpleCookie")
    .AddCookie("SimpleCookie", options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.Name = "MonitoringAuth";
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
    Role adminRole = new Role { RoleName = "Admin", UsersList = usersLIst };
    Role clientRole = new Role { RoleName = "Client", UsersList = usersLIst };
    // создаем два объекта User
    User user1 = new User { Name = "Admin1", SecondName = "Admin", Password = "admin", UserRole = adminRole };
    User user2 = new User { Name = "Marat", SecondName = "Ismagilov", Password = "marat321", UserRole = clientRole };
    usersLIst.Add(user1);
    usersLIst.Add(user2);

    // добавляем их в бд
    Monitiordb.Roles.AddRange(adminRole, clientRole);
    Monitiordb.Users.AddRange(user1, user2);
    Monitiordb.SaveChanges();
}
// получение данных

app.Run();
