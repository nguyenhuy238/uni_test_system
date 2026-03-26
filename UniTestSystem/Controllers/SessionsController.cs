using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class SessionsController : Controller
    {
        private readonly ISessionService _sessionService;
        private readonly IGradingService _gradingService;

        public SessionsController(ISessionService sessionService,
                                  IGradingService gradingService)
        {
            _sessionService = sessionService;
            _gradingService = gradingService;
        }

        [HttpGet("/mytests/start/{testId}")]
        public async Task<IActionResult> Start(string testId, string? scheduleId = null, string? accessToken = null)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid)) return Forbid();

            var result = await _sessionService.StartSessionAsync(new StartSessionCommand
            {
                TestId = testId,
                UserId = uid,
                ScheduleId = scheduleId,
                AccessToken = accessToken,
                BlockRestartAfterSubmit = true,
                IncludeQuestionPayload = false,
                ReturnNotFoundForUnavailableTest = true,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict)
            {
                TempData["Err"] = result.ErrorCode switch
                {
                    "ACTIVE_ON_OTHER_DEVICE" => "Bạn đang có phiên làm bài trên thiết bị khác. Vui lòng kết thúc phiên đó trước khi bắt đầu.",
                    "SESSION_BOUND_OTHER_DEVICE" => "Phiên đang làm đã được gắn với thiết bị khác.",
                    _ => "Không thể bắt đầu phiên làm bài trên thiết bị này."
                };
                return RedirectToAction("Index", "MyTests");
            }

            if (result.Data == null) return Forbid();
            if (result.Data.IsLatestSubmitted)
            {
                return Redirect($"/mytests/result/{result.Data.SessionId}");
            }

            return Redirect($"/mytests/session/{result.Data.SessionId}");
        }

        [HttpGet("/mytests/session/{id}")]
        public async Task<IActionResult> Runner(string id)
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (uid == null) return Forbid();

            var result = await _sessionService.ResumeSessionAsync(new ResumeSessionCommand
            {
                SessionId = id,
                UserId = uid,
                RequestContext = BuildRequestContext()
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Status == SessionServiceStatus.Conflict)
            {
                TempData["Err"] = "Phiên làm bài này thuộc về thiết bị khác.";
                return RedirectToAction("Index", "MyTests");
            }

            if (result.Data == null) return NotFound();

            ViewBag.TestTitle = result.Data.TestTitle;
            ViewBag.Duration = result.Data.DurationMinutes;
            ViewBag.RemainingSeconds = result.Data.RemainingSeconds;
            return View(result.Data.Session);
        }

        [HttpGet("/mytests/result/{id}")]
        public async Task<IActionResult> Result(string id)
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (uid == null) return Forbid();

            var result = await _sessionService.GetSessionResultAsync(new GetSessionResultCommand
            {
                SessionId = id,
                UserId = uid
            });

            if (result.Status == SessionServiceStatus.NotFound) return NotFound();
            if (result.Status == SessionServiceStatus.Forbidden) return Forbid();
            if (result.Data == null) return NotFound();

            ViewBag.TestTitle = result.Data.TestTitle;
            ViewBag.HasPendingRegrade = result.Data.HasPendingRegrade;
            return View(result.Data.Session);
        }

        [HttpPost("/mytests/regrade/request")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRegrade(string sessionId, string reason)
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(uid)) return Forbid();

            try
            {
                await _gradingService.RequestRegradeAsync(
                    sessionId,
                    uid,
                    reason,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["Msg"] = "Yêu cầu phúc khảo đã được gửi.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = "Không thể gửi yêu cầu phúc khảo: " + ex.Message;
            }

            return Redirect($"/mytests/result/{sessionId}");
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

