using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Application;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class UserSessionsController : Controller
    {
        private readonly AuthService _auth;

        public UserSessionsController(AuthService auth)
        {
            _auth = auth;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("/sessions")]
        public async Task<IActionResult> Index()
        {
            var sessions = await _auth.GetActiveSessionsAsync(CurrentUserId);
            return View(sessions);
        }

        [HttpPost("/sessions/revoke")]
        public async Task<IActionResult> Revoke(string sessionId)
        {
            await _auth.RevokeSessionAsync(sessionId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/sessions/revoke-all")]
        public async Task<IActionResult> RevokeAll()
        {
            await _auth.RevokeAllSessionsAsync(CurrentUserId);
            return RedirectToAction(nameof(Index));
        }
    }
}
