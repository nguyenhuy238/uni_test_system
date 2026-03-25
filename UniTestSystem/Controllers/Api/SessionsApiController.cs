using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using System.Security.Claims;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api
{
    [ApiController]
    [Authorize]
    [Route("api/internal/sessions")]
    public class SessionsApiController : ControllerBase
    {
        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly SessionDeviceGuardService _sessionDeviceGuard;

        public SessionsApiController(IRepository<Session> sRepo, IRepository<Test> tRepo, SessionDeviceGuardService sessionDeviceGuard)
        {
            _sRepo = sRepo;
            _tRepo = tRepo;
            _sessionDeviceGuard = sessionDeviceGuard;
        }

        // Helper: tính remaining giây theo Session + Test
        private async Task<int> GetRemainingSecondsAsync(Session s)
        {
            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId);
            var duration = Math.Max(1, t?.DurationMinutes ?? 30);
            var total = duration * 60;

            var runningDelta = s.TimerStartedAt.HasValue
                ? (int)Math.Floor((DateTime.UtcNow - s.TimerStartedAt.Value).TotalSeconds)
                : 0;

            var consumed = Math.Max(0, s.ConsumedSeconds + runningDelta);
            return Math.Max(0, total - consumed);
        }

        // POST /api/tests/sessions/{id}/touch (giữ nguyên)
        [HttpPost("{id}/touch")]
        public async Task<IActionResult> Touch(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            s.LastActivityAt = DateTime.UtcNow;
            await _sRepo.UpsertAsync(x => x.Id == id, s);
            var remaining = await GetRemainingSecondsAsync(s);
            return Ok(new { ok = true, at = s.LastActivityAt, remainingSeconds = remaining, running = s.TimerStartedAt.HasValue });
        }

        // ===== NEW: RESUME =====
        [HttpPost("{id}/resume")]
        public async Task<IActionResult> Resume(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            if (!s.TimerStartedAt.HasValue)
            {
                s.TimerStartedAt = DateTime.UtcNow;
            }
            s.LastActivityAt = DateTime.UtcNow;

            await _sRepo.UpsertAsync(x => x.Id == id, s);
            var remaining = await GetRemainingSecondsAsync(s);
            return Ok(new { ok = true, remainingSeconds = remaining, running = true });
        }

        // ===== NEW: PAUSE =====
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> Pause(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            if (s.TimerStartedAt.HasValue)
            {
                var delta = (int)Math.Floor((DateTime.UtcNow - s.TimerStartedAt.Value).TotalSeconds);
                if (delta > 0) s.ConsumedSeconds += delta;
                s.TimerStartedAt = null;
            }
            s.LastActivityAt = DateTime.UtcNow;

            await _sRepo.UpsertAsync(x => x.Id == id, s);
            var remaining = await GetRemainingSecondsAsync(s);
            return Ok(new { ok = true, remainingSeconds = remaining, running = false });
        }

        private async Task<bool> EnsureSessionDeviceAsync(Session session)
        {
            var requestFp = _sessionDeviceGuard.GetRequestFingerprint(
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return await _sessionDeviceGuard.EnsureSessionDeviceAsync(
                session.Id,
                requestFp,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }
}
