using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Database.Roles;
using MonitoringServiceCore.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MonitoringServiceCore.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthorizeService _authService;

        [BindProperty]
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Введите корректный email адрес")]
        public string? Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Введите пароль")]
        public required string Password { get; set; }

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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = _authService.GetUserByEmail(Email!, Password);

            if (user == null)
            {
                ErrorMessage = "Неверный email или пароль";
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