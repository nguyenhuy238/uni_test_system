using UniTestSystem.Application.Interfaces;
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
        private readonly ISessionService _sessionService;

        public TestsApiController(
            ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        // ====================== START ======================
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(string id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.StartSessionAsync(new StartSessionCommand
            {
                TestId = id,
                UserId = uid,
                BlockRestartAfterSubmit = false,
                IncludeQuestionPayload = true,
                ReturnNotFoundForUnavailableTest = false,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.Forbidden || result.Status == SessionServiceStatus.NotFound)
            {
                return Forbid();
            }

            if (result.Status == SessionServiceStatus.Conflict)
            {
                var message = result.ErrorCode switch
                {
                    "ACTIVE_ON_OTHER_DEVICE" => "An in-progress session is active on another device.",
                    "SESSION_BOUND_OTHER_DEVICE" => "Session is bound to another device.",
                    _ => "Cannot start session."
                };
                return Conflict(new { message });
            }

            if (result.Data == null)
            {
                return BadRequest(new { message = result.Message ?? "Cannot start session." });
            }

            return Ok(new
            {
                sessionId = result.Data.SessionId,
                questions = result.Data.Questions.Select(x => new { x.Id, x.Type, x.Content, x.Options }),
                durationMinutes = result.Data.DurationMinutes,
                remainingMinutes = (int)Math.Ceiling(result.Data.RemainingSeconds / 60.0)
            });
        }

        // ====================== SUBMIT ======================
        public class SubmitPayload { public Dictionary<string, string?> Answers { get; set; } = new(); }

        public class SaveAnswerPayload
        {
            public Dictionary<string, string?> Answers { get; set; } = new();
            public DateTime? ClientTimestamp { get; set; }
            public Dictionary<string, DateTime?> QuestionClientTimestamps { get; set; } = new();
        }

        [HttpPost("sessions/{sid}/save-answer")]
        [HttpPost("/sessions/{sid}/save-answer")]
        public async Task<IActionResult> SaveAnswer(string sid, [FromBody] SaveAnswerPayload p)
        {
            return await SaveAnswerCoreAsync(sid, p);
        }

        [HttpPost("sessions/{sid}/save-draft")]
        public async Task<IActionResult> SaveDraft(string sid, [FromBody] SaveAnswerPayload p)
        {
            return await SaveAnswerCoreAsync(sid, p);
        }

        private async Task<IActionResult> SaveAnswerCoreAsync(string sid, SaveAnswerPayload p)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.SaveAnswerAsync(new SaveAnswerCommand
            {
                SessionId = sid,
                UserId = uid,
                Answers = p.Answers,
                ClientTimestamp = p.ClientTimestamp,
                QuestionClientTimestamps = p.QuestionClientTimestamps,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict)
            {
                var message = result.ErrorCode switch
                {
                    "SESSION_ALREADY_SUBMITTED" => "Session was already submitted.",
                    "SESSION_BOUND_OTHER_DEVICE" => "Session is bound to another device.",
                    _ => "Cannot save answer."
                };
                return Conflict(new { message, code = result.ErrorCode });
            }
            if (result.Status == SessionServiceStatus.BadRequest) return BadRequest(new { message = "Session is not active" });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot save draft." });

            return Ok(new { ok = true, updatedCount = result.Data.UpdatedCount, at = result.Data.At });
        }

        [HttpPost("sessions/{sid}/submit")]
        public async Task<IActionResult> Submit(string sid, [FromBody] SubmitPayload p)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.SubmitSessionAsync(new SubmitSessionCommand
            {
                SessionId = sid,
                UserId = uid,
                Answers = p.Answers,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot submit session." });

            return Ok(new
            {
                result.Data.Id,
                result.Data.TotalScore,
                result.Data.MaxScore,
                result.Data.Percent,
                result.Data.IsPassed,
                result.Data.Status
            });
        }

        // ====================== PAUSE ======================
        [HttpPost("sessions/{sid}/pause")]
        public async Task<IActionResult> Pause(string sid)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.PauseSessionAsync(new SessionTimerCommand
            {
                SessionId = sid,
                UserId = uid,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot pause session." });

            return Ok(new { remainingSeconds = result.Data.RemainingSeconds, running = result.Data.Running });
        }

        // ====================== RESUME ======================
        [HttpPost("sessions/{sid}/resume")]
        public async Task<IActionResult> Resume(string sid)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.ResumeTimerAsync(new SessionTimerCommand
            {
                SessionId = sid,
                UserId = uid,
                RequireInProgressState = true,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });
            if (result.Data == null) return BadRequest(new { message = result.Message ?? "Cannot resume session." });

            return Ok(new { remainingSeconds = result.Data.RemainingSeconds, running = result.Data.Running });
        }

        // ====================== ANTI-CHEAT LOGS ======================
        [HttpPost("sessions/{sid}/log")]
        public async Task<IActionResult> LogEvent(string sid, [FromBody] SessionLogEvent model)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            var result = await _sessionService.LogEventAsync(new SessionLogEventCommand
            {
                SessionId = sid,
                UserId = uid,
                ActionType = model.ActionType,
                Detail = model.Detail,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict) return Conflict(new { message = "Session is bound to another device." });

            return Ok();
        }

        public class SessionLogEvent
        {
            public string ActionType { get; set; } = "";
            public string? Detail { get; set; }
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

