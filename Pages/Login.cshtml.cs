using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace SignalRTest.Views.Login
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public void OnGet(string? returnUrl = null)
        {
            // default to home if not provided
            Input.ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var ok = string.Equals(Input.UserNameOrEmail, "asd", StringComparison.OrdinalIgnoreCase)
                  && string.Equals(Input.Password, "asd", StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return Page();
            }

            HttpContext.Session.SetString("email", Input.UserNameOrEmail);

            // Sign in with cookie auth
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Input.UserNameOrEmail),
            new Claim(ClaimTypes.Name, Input.UserNameOrEmail),
            new Claim(ClaimTypes.Email, Input.UserNameOrEmail)
        };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProps);



            return RedirectToPage("/TestPanel");
        }
    }
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or username")]
        public string UserNameOrEmail { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string? ReturnUrl { get; set; }

    }

}
