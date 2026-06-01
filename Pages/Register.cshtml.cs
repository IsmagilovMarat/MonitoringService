using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database;
using MonitoringServiceCore.Database.dbContext;
using MonitoringServiceCore.Database.Roles;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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

            // Проверка валидности email
            if (!IsValidEmail(Input.Email))
            {
                ErrorMessage = "Введите корректный email адрес";
                return Page();
            }

            // Проверка, существует ли пользователь с таким именем
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.Name == Input.Username);
            if (existingUser != null)
            {
                ErrorMessage = "Пользователь с таким именем уже существует";
                return Page();
            }

            // Проверка, существует ли пользователь с таким email
            var existingEmail = _dbContext.Users.FirstOrDefault(u => u.Email == Input.Email);
            if (existingEmail != null)
            {
                ErrorMessage = "Пользователь с таким email уже зарегистрирован";
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
                Id = Guid.NewGuid(),
                Name = Input.Username,
                SecondName = Input.SecondName ?? string.Empty,
                Email = Input.Email,
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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при регистрации: {ex.Message}";
            }

            return Page();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Простая проверка email через Regex
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
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

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Введите корректный email адрес")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

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