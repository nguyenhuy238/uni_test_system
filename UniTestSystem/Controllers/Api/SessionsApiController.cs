using UniTestSystem.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api
{
    [ApiController]
    [Authorize]
    [Route("api/internal/sessions")]
    public class SessionsApiController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsApiController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        // POST /api/tests/sessions/{id}/touch (giữ nguyên)
        [HttpPost("{id}/touch")]
        public async Task<IActionResult> Touch(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.TouchSessionAsync(new SessionTouchCommand
            {
                SessionId = id,
                UserId = uid,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot touch session." });

            return Ok(new
            {
                ok = true,
                at = result.Data.At,
                remainingSeconds = result.Data.RemainingSeconds,
                running = result.Data.Running
            });
        }

        // ===== NEW: RESUME =====
        [HttpPost("{id}/resume")]
        public async Task<IActionResult> Resume(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.ResumeTimerAsync(new SessionTimerCommand
            {
                SessionId = id,
                UserId = uid,
                RequireInProgressState = false,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot resume session." });

            return Ok(new { ok = true, remainingSeconds = result.Data.RemainingSeconds, running = result.Data.Running });
        }

        // ===== NEW: PAUSE =====
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> Pause(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.PauseSessionAsync(new SessionTimerCommand
            {
                SessionId = id,
                UserId = uid,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot pause session." });

            return Ok(new { ok = true, remainingSeconds = result.Data.RemainingSeconds, running = result.Data.Running });
        }

        private SessionRequestContext BuildRequestContext()
        {
            return new SessionRequestContext
            {
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
        }
    }
}

