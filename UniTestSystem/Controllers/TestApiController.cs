using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [ApiController]
    [Route("api/tests")]
    [Authorize]
    public class TestsApiController : ControllerBase
    {
        private readonly TestService _svc;
        private readonly AssessmentService _assessSvc;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<Question> _qRepo;
        private readonly IRepository<SessionLog> _slRepo;
        private readonly SessionDeviceGuardService _sessionDeviceGuard;

        public TestsApiController(
            TestService svc,
            AssessmentService assessSvc,
            IRepository<Test> tRepo,
            IRepository<Session> sRepo,
            IRepository<Question> qRepo,
            IRepository<SessionLog> slRepo,
            SessionDeviceGuardService sessionDeviceGuard)
        {
            _svc = svc;
            _assessSvc = assessSvc;
            _tRepo = tRepo;
            _sRepo = sRepo;
            _qRepo = qRepo;
            _slRepo = slRepo;
            _sessionDeviceGuard = sessionDeviceGuard;
        }

        // ====================== START ======================
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allowed = await _assessSvc.GetAvailableTestIdsAsync(uid, DateTime.UtcNow);
            if (!allowed.Contains(id)) return Forbid();

            var requestFp = _sessionDeviceGuard.GetRequestFingerprint(Request, HttpContext.Connection.RemoteIpAddress?.ToString());
            var hasOtherDeviceSession = await _sessionDeviceGuard.HasActiveSessionOnOtherDeviceAsync(uid, requestFp);
            if (hasOtherDeviceSession)
            {
                return Conflict(new { message = "An in-progress session is active on another device." });
            }

            // Tạo mới hoặc trả về session đang làm (service của bạn)
            var s = await _svc.StartAsync(id, uid);
            var bound = await _sessionDeviceGuard.EnsureSessionDeviceAsync(
                s.Id,
                requestFp,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!bound)
            {
                return Conflict(new { message = "Session is bound to another device." });
            }

            var test = await _tRepo.FirstOrDefaultAsync(t => t.Id == id);
            var duration = test?.DurationMinutes ?? 30;

            var remainingSeconds = ComputeRemainingSeconds(s, duration);

            // Fetch questions from StudentAnswers
            var allQuestions = await _qRepo.GetAllAsync();
            var qMap = allQuestions.ToDictionary(x => x.Id, x => x);
            var q = s.StudentAnswers
                .Select(sa => qMap.TryGetValue(sa.QuestionId, out var question) ? question : null)
                .Where(x => x != null)
                .Select(x => new { x!.Id, x.Type, x.Content, x.Options });

            return Ok(new
            {
                sessionId = s.Id,
                questions = q,
                durationMinutes = duration,
                remainingMinutes = (int)Math.Ceiling(remainingSeconds / 60.0)
            });
        }

        // ====================== SUBMIT ======================
        public class SubmitPayload { public Dictionary<string, string?> Answers { get; set; } = new(); }

        public class SaveDraftPayload { public Dictionary<string, string?> Answers { get; set; } = new(); }

        [HttpPost("sessions/{sid}/save-draft")]
        public async Task<IActionResult> SaveDraft(string sid, [FromBody] SaveDraftPayload p)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sid);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });
            if (s.Status != SessionStatus.InProgress)
                return BadRequest(new { message = "Session is not active" });

            var allQuestions = await _qRepo.GetAllAsync();
            var qMap = allQuestions.ToDictionary(x => x.Id, x => x);

            var updated = 0;
            var now = DateTime.UtcNow;

            foreach (var sa in s.StudentAnswers)
            {
                if (!p.Answers.TryGetValue(sa.QuestionId, out var raw)) continue;
                if (!qMap.TryGetValue(sa.QuestionId, out var q)) continue;

                var value = (raw ?? string.Empty).Trim();
                switch (q.Type)
                {
                    case QType.MCQ:
                        sa.SelectedOptionId = string.IsNullOrEmpty(value) ? null : value;
                        break;
                    case QType.TrueFalse:
                        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) value = "True";
                        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) value = "False";
                        sa.SelectedOptionId = string.IsNullOrEmpty(value) ? null : value;
                        break;
                    case QType.Essay:
                        sa.EssayAnswer = value;
                        break;
                    default:
                        sa.EssayAnswer = value;
                        break;
                }

                sa.AnsweredAt = now;
                updated++;
            }

            s.LastActivityAt = now;
            await _sRepo.UpsertAsync(x => x.Id == s.Id, s);

            return Ok(new { ok = true, updatedCount = updated, at = now });
        }

        [HttpPost("sessions/{sid}/submit")]
        public async Task<IActionResult> Submit(string sid, [FromBody] SubmitPayload p)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var s0 = await _sRepo.FirstOrDefaultAsync(x => x.Id == sid);
            if (s0 == null) return NotFound();
            if (!string.Equals(s0.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s0)) return Conflict(new { message = "Session is bound to another device." });

            var s = await _svc.SubmitAsync(sid, p.Answers);

            return Ok(new
            {
                s.Id,
                s.TotalScore,
                s.MaxScore,
                s.Percent,
                s.IsPassed,
                s.Status
            });
        }

        // ====================== PAUSE ======================
        [HttpPost("sessions/{sid}/pause")]
        public async Task<IActionResult> Pause(string sid)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sid);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId);
            if (t == null) return NotFound();

            if (s.TimerStartedAt.HasValue)
            {
                var delta = (int)Math.Floor((DateTime.UtcNow - s.TimerStartedAt.Value).TotalSeconds);
                s.ConsumedSeconds = Math.Max(0, s.ConsumedSeconds + Math.Max(0, delta));
                s.TimerStartedAt = null;
                s.LastActivityAt = DateTime.UtcNow;
                await _sRepo.UpsertAsync(x => x.Id == s.Id, s);
            }

            var remaining = ComputeRemainingSeconds(s, t.DurationMinutes);
            return Ok(new { remainingSeconds = remaining, running = false });
        }

        // ====================== RESUME ======================
        [HttpPost("sessions/{sid}/resume")]
        public async Task<IActionResult> Resume(string sid)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sid);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId);
            if (t == null) return NotFound();

            var remaining = ComputeRemainingSeconds(s, t.DurationMinutes);
            if (remaining <= 0 || s.Status != SessionStatus.InProgress)
                return Ok(new { remainingSeconds = Math.Max(0, remaining), running = false });

            if (!s.TimerStartedAt.HasValue)
            {
                s.TimerStartedAt = DateTime.UtcNow;
                s.LastActivityAt = DateTime.UtcNow;
                await _sRepo.UpsertAsync(x => x.Id == s.Id, s);
            }

            remaining = ComputeRemainingSeconds(s, t.DurationMinutes);
            return Ok(new { remainingSeconds = remaining, running = true });
        }

        // ====================== ANTI-CHEAT LOGS ======================
        [HttpPost("sessions/{sid}/log")]
        public async Task<IActionResult> LogEvent(string sid, [FromBody] SessionLogEvent model)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sid);
            if (s == null) return NotFound();
            if (!string.Equals(s.UserId, uid, StringComparison.Ordinal)) return Forbid();
            if (!await EnsureSessionDeviceAsync(s)) return Conflict(new { message = "Session is bound to another device." });

            var log = new SessionLog
            {
                Id = Guid.NewGuid().ToString("N"),
                SessionId = sid,
                ActionType = model.ActionType,
                Detail = model.Detail,
                Timestamp = DateTime.UtcNow,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _slRepo.InsertAsync(log);
            return Ok();
        }

        public class SessionLogEvent
        {
            public string ActionType { get; set; } = "";
            public string? Detail { get; set; }
        }

        // ====================== Helpers ======================
        private static int ComputeRemainingSeconds(Session s, int durationMinutes)
        {
            var total = Math.Max(1, durationMinutes) * 60;
            var runningDelta = s.TimerStartedAt.HasValue
                ? (int)Math.Floor((DateTime.UtcNow - s.TimerStartedAt.Value).TotalSeconds)
                : 0;
            var consumed = Math.Max(0, s.ConsumedSeconds + Math.Max(0, runningDelta));
            return Math.Max(0, total - consumed);
        }

        private async Task<bool> EnsureSessionDeviceAsync(Session session)
        {
            var requestFp = _sessionDeviceGuard.GetRequestFingerprint(Request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return await _sessionDeviceGuard.EnsureSessionDeviceAsync(
                session.Id,
                requestFp,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
    }
}
