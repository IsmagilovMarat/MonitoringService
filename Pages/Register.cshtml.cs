using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using System.ComponentModel.DataAnnotations;

namespace MonitoringServiceCore.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly MonitoringDbContext _dbContext;

        public RegisterModel(MonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Проверка, существует ли пользователь с таким именем
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.Name == Input.Username);
            if (existingUser != null)
            {
                ErrorMessage = "Пользователь с таким именем уже существует";
                return Page();
            }

            // Проверка, совпадают ли пароли
            if (Input.Password != Input.ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return Page();
            }

            // Получаем роль "Client" по умолчанию
            var clientRole = _dbContext.Roles.FirstOrDefault(r => r.RoleName == "Client");
            if (clientRole == null)
            {
                ErrorMessage = "Ошибка системы: роль Client не найдена";
                return Page();
            }

            // Создаем нового пользователя
            var newUser = new User
            {
                Name = Input.Username,
                SecondName = Input.SecondName ?? string.Empty,
                Password = Input.Password, // В реальном проекте нужно хешировать пароль!
                UserRole = clientRole,
                RoleId = clientRole.Id
            };

            try
            {
                _dbContext.Users.Add(newUser);
                await _dbContext.SaveChangesAsync();
                SuccessMessage = "Регистрация прошла успешно! Теперь вы можете войти в систему.";

                // Очищаем форму
                Input = new RegisterInputModel();

                // Можно автоматически перенаправить на страницу входа через несколько секунд
                // return RedirectToPage("Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при регистрации: {ex.Message}";
            }

            return Page();
        }
    }

    public class RegisterInputModel
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        [Display(Name = "Имя пользователя")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно содержать от 3 до 50 символов")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Фамилия")]
        [StringLength(50, ErrorMessage = "Фамилия не может быть длиннее 50 символов")]
        public string? SecondName { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [Display(Name = "Пароль")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль должен содержать от 4 до 100 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтвердите пароль")]
        [Display(Name = "Подтверждение пароля")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}