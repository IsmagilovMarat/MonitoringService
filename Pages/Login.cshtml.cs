using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
using System.Security.Claims;

namespace MonitoringServiceCore.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthorizeService _authService;

        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public required string Password { get; set; }

        [BindProperty]
        public bool UseEmailLogin { get; set; } = false;

        public string? ErrorMessage { get; set; }

        public LoginModel(AuthorizeService authService)
        {
            _authService = authService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            User? user = null;

            // Вход по email или по имени пользователя
            if (UseEmailLogin && !string.IsNullOrEmpty(Email))
            {
                user = _authService.GetUserByEmail(Email, Password);
                if (user == null)
                {
                    ErrorMessage = "Неверный email или пароль";
                    return Page();
                }
            }
            else if (!string.IsNullOrEmpty(Username))
            {
                user = _authService.GetUserFromDb(Username, Password);
                if (user == null)
                {
                    ErrorMessage = "Неверное имя пользователя или пароль";
                    return Page();
                }
            }
            else
            {
                ErrorMessage = "Введите имя пользователя или email";
                return Page();
            }

            string userRoleName = user.UserRole?.RoleName ?? "";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", userRoleName),
                new Claim("UserName", user.Name ?? "")
            };

            var identity = new ClaimsIdentity(claims, "SimpleCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("SimpleCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true
            });

            if (userRoleName == "Admin")
            {
                return RedirectToPage("Index");
            }
            else
            {
                return RedirectToPage("UsersMainPage");
            }
        }
    }
}