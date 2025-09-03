//using Microsoft.AspNetCore.Mvc;

//namespace SignalRTest.Controllers
//{

//    [Route("test-panel")]
//    public class TestPanelController : Controller
//    {
//        [HttpGet("")]
//        public IActionResult Index()
//        {
//            var email = HttpContext.Session.GetString("email");
//            if (string.IsNullOrEmpty(email))
//                return RedirectToAction("Login", "Login",
//                    new { returnUrl = Url.Action("Index", "TestPanel") });

//            ViewBag.Email = email;
//            return View("TestPanel"); // Views/TestPanel/Index.cshtml
//        }

//        [HttpPost("")]
//        public IActionResult Post([FromBody] TestPanelForm form)
//        {
//            if (string.IsNullOrWhiteSpace(form?.UserId))
//                return BadRequest(new { error = "UserId is required" });

//            return Ok(new
//            {
//                ok = true,
//                received = form,
//                serverTimeUtc = DateTime.UtcNow
//            });
//        }
//    }

//    public class TestPanelForm
//    {
//        public string UserId { get; set; } = "";
//        public string? Note { get; set; }
//    }

//}
