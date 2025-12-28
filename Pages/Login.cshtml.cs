using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Services;
using System.Security.Claims;

namespace MonitoringServiceCore.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Проверяем пользователя
            var user = _authService.CheckUser(Username, Password);

            if (user == null)
            {
                ErrorMessage = "Неверное имя пользователя или пароль";
                return Page();
            }

            // Создаем простые claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.UserRole?.RoleName ?? "Client")
            };

            // Создаем identity
            var identity = new ClaimsIdentity(claims, "SimpleCookie");
            var principal = new ClaimsPrincipal(identity);

            // Входим в систему
            await HttpContext.SignInAsync("SimpleCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true // Куки сохраняются после закрытия браузера
            });

            // Перенаправляем на главную
            return RedirectToPage("/Index");
        }
    }
}