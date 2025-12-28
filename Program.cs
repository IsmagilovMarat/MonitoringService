using System.Net.Security;
using System.Text.RegularExpressions;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Pages;
using MonitoringServiceCore.Services;


using System;
using System.IO;
using System.Text.RegularExpressions;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<MonitoringDbContext>();
builder.Services.AddSingleton<AuthService>();

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
    Role adminRole = new Role { RoleName = "Admin",UsersList = usersLIst};
    Role clientRole = new Role { RoleName = "Client",UsersList = usersLIst };
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
var url = "https://metanit.com/sharp/aspnet6/3.8.php";

using var httpClient = new HttpClient();

try
{
    // Устанавливаем User-Agent для имитации браузера
    httpClient.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

    // Получаем HTML-контент
    var html = await httpClient.GetStringAsync(url);

    Console.WriteLine(html);

    // Если нужно сохранить в файл
    await File.WriteAllTextAsync("metanit_page.html", html);
    Console.WriteLine("\nHTML сохранен в metanit_page.html");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Ошибка при запросе: {ex.Message}");
}



   
        string filePath = "metanit_page.html"; // путь к вашему файлу

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Файл {filePath} не найден!");
            return;
        }

        try
        {
            // Читаем весь файл
            string content = File.ReadAllText(filePath);

            // Способ 1: Использование StringComparison (без учета регистра)
            int count1 = CountOccurrencesIgnoreCase(content, "NET");
            Console.WriteLine($"Способ 1 (без учета регистра): найдено {count1} вхождений слова 'NET'");

            // Способ 2: Использование регулярных выражений
            int count2 = CountWithRegex(content, @"\bNET\b", RegexOptions.IgnoreCase);
            Console.WriteLine($"Способ 2 (целые слова): найдено {count2} вхождений слова 'NET'");

            // Способ 3: Простой поиск с учетом регистра
            int count3 = CountOccurrences(content, "NET");
            Console.WriteLine($"Способ 3 (с учетом регистра): найдено {count3} вхождений слова 'NET'");

            // Дополнительно: искать все варианты (.NET, ASP.NET и т.д.)
            int count4 = CountWithRegex(content, @"\.?NET\b", RegexOptions.IgnoreCase);
            Console.WriteLine($"Все варианты (.NET, NET): найдено {count4} вхождений");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке файла: {ex.Message}");
        }
    

    // Метод 1: Без учета регистра
    static int CountOccurrencesIgnoreCase(string text, string searchWord)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(searchWord, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += searchWord.Length;
        }

        return count;
    }

    // Метод 2: С учетом регистра
    static int CountOccurrences(string text, string searchWord)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(searchWord, index)) != -1)
        {
            count++;
            index += searchWord.Length;
        }

        return count;
    }

    // Метод 3: С использованием регулярных выражений
    static int CountWithRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        return Regex.Matches(text, pattern, options).Count;
    }

app.Run();
