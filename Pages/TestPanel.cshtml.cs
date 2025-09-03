using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using SignalRTest.Hubs;
using SignalRTest.Services;

namespace SignalRTest.Pages
{
    [Authorize]
    public class TestPanelModel : PageModel
    {
        private readonly IHubContext<ChatHub> _hub;

        public TestPanelModel(IHubContext<ChatHub> hub)
            => _hub = hub;

        [BindProperty]
        public TestPanelViewModel Input { get; set; } = new();

        public void OnGet(string? returnUrl = null)
            => Input.ReturnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl;

        // Handle the Connect action (set session userId)
        [ValidateAntiForgeryToken]
        public IActionResult OnPostConnect([FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required.");

            HttpContext.Session.SetString("UserId", userId);
            return new JsonResult(new { ok = true, message = $"Session userId set to {userId}" });
        }

        // Handle the Test action (send test data to specific user)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostTest([FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required.");

            // Check if the target user is online by checking the UserConnectionManagerService
            var connectionManager = HttpContext.RequestServices.GetRequiredService<UserConnectionManagerService>();
            if (!connectionManager.IsOnline(userId))
            {
                return new JsonResult(new
                {
                    ok = false,
                    message = $"User '{userId}' is not online",
                    targetUser = userId
                });
            }

            // Create sample test JSON data
            var testData = new
            {
                messageId = Guid.NewGuid().ToString(),
                text = "This is a test message from TestPanel",
                sentBy = User?.Identity?.Name ?? "TestPanel",
                sentAt = DateTimeOffset.UtcNow,
                targetUser = userId,
                data = new
                {
                    temperature = 23.5,
                    humidity = 65,
                    status = "active",
                    items = new[] { "item1", "item2", "item3" }
                },
                metadata = new
                {
                    source = "TestPanel",
                    version = "1.0",
                    priority = "normal"
                }
            };

            // Send to the specific target user via SignalR
            await _hub.Clients.User(userId).SendAsync("Test", testData);

            return new JsonResult(new
            {
                ok = true,
                message = $"Test data sent successfully to user '{userId}'",
                targetUser = userId,
                dataSent = testData
            });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Kill the auth cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear any of your session state
            HttpContext.Session.Clear();

            // (Optional) belt-and-suspenders:
            Response.Cookies.Delete(".AspNetCore.Cookies");

            return RedirectToPage("/Login"); // or wherever your login page/home is
        }
    }

    public class TestPanelViewModel
    {
        public string? Email { get; set; }
        public string ReturnUrl { get; internal set; } = "";
    }
}