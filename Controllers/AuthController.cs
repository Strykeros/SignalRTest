using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace SignalRTest.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("validate-login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var isValid = string.Equals(request.UserNameOrEmail, "asd", StringComparison.OrdinalIgnoreCase)
                       && string.Equals(request.Password, "asd", StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            // Store in session
            HttpContext.Session.SetString("email", request.UserNameOrEmail);

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.UserNameOrEmail),
                new Claim(ClaimTypes.Name, request.UserNameOrEmail),
                new Claim(ClaimTypes.Email, request.UserNameOrEmail)
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

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                RedirectUrl = string.IsNullOrEmpty(request.ReturnUrl) ? "/" : request.ReturnUrl,
                User = new UserInfo
                {
                    Email = request.UserNameOrEmail,
                    Name = request.UserNameOrEmail
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Clear the session
                HttpContext.Session.Clear();

                // Sign out from cookie authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Ok(new LogoutResponse
                {
                    Success = true,
                    Message = "Logged out successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LogoutResponse
                {
                    Success = false,
                    Message = "Logout failed",
                    Error = ex.Message
                });
            }
        }


        public class LoginRequest
        {
            [Required]
            [Display(Name = "Email or username")]
            public string UserNameOrEmail { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            public string? ReturnUrl { get; set; }
        }

        public class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string? RedirectUrl { get; set; }
            public UserInfo? User { get; set; }
            public List<string>? Errors { get; set; }
        }

        public class UserInfo
        {
            public string Email { get; set; } = "";
            public string Name { get; set; } = "";
            public bool IsAuthenticated { get; set; }
        }

        public class LogoutResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Error { get; set; }
        }

    }


}
