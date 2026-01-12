using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitoringServiceCore.Services;
using System.Security.Claims;

namespace MonitoringServiceCore.Pages
{
    public class LoginModel : PageModel
    {
        private  AuthorizeService _authService;

        [BindProperty]
        public required string Username { get; set; }

        [BindProperty]
        public required string Password { get; set; }


        public LoginModel(AuthorizeService authService)
        {
            _authService = authService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Проверяем пользователя

              var user = _authService.GetUserFromDb(Username, Password);

            if (user == null)
            {
                return Page();
            }
            string userRoleName;

            if(user.UserRole.RoleName != null)
            {
                 userRoleName = user.UserRole.RoleName.ToString();
            }
            else
            {
                 userRoleName = "";
            }



            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.UserRole?.RoleName?? "no userRoleName")
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