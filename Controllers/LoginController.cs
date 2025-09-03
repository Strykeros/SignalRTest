using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace SignalRTest.Controllers
{
    [Route("login")]
    public class LoginController : Controller
    {
        //[HttpGet("")]
        //public IActionResult Login(string? returnUrl = null) =>
        //    View(new LoginViewModel { ReturnUrl = returnUrl });

        //[HttpPost("", Name = "LoginPost")]
        //[ValidateAntiForgeryToken]
        //public IActionResult Login(LoginViewModel vm)
        //{
        //    if (!ModelState.IsValid) return View(vm);

        //    var ok = string.Equals(vm.UserNameOrEmail, "asd", StringComparison.OrdinalIgnoreCase)
        //             && string.Equals(vm.Password, "asd", StringComparison.OrdinalIgnoreCase);

        //    if (!ok)
        //    {
        //        ModelState.AddModelError("", "Invalid username or password");
        //        return View(vm);
        //    }

        //    // --- SESSION (no DB) ---
        //    HttpContext.Session.SetString("email", vm.UserNameOrEmail);



        //    // PRG pattern: redirect instead of returning a View on POST
        //    return RedirectToAction("Index", "TestPanel");
        //}

    }


}
