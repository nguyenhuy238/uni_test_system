using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class SessionsController : Controller
    {
        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly TestService _testSvc;
        private readonly AssessmentService _assessSvc;
        private readonly ExamAccessTokenService _examAccessTokenService;
        private readonly SessionDeviceGuardService _sessionDeviceGuardService;
        private readonly IGradingService _gradingService;

        public SessionsController(IRepository<Session> s,
                                  IRepository<Test> t,
                                  TestService testSvc,
                                  AssessmentService assessSvc,
                                  ExamAccessTokenService examAccessTokenService,
                                  SessionDeviceGuardService sessionDeviceGuardService,
                                  IGradingService gradingService)
        {
            _sRepo = s;
            _tRepo = t;
            _testSvc = testSvc;
            _assessSvc = assessSvc;
            _examAccessTokenService = examAccessTokenService;
            _sessionDeviceGuardService = sessionDeviceGuardService;
            _gradingService = gradingService;
        }

        [HttpGet("/mytests/start/{testId}")]
        public async Task<IActionResult> Start(string testId, string? scheduleId = null, string? accessToken = null)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid)) return Forbid();

            var test = await _tRepo.FirstOrDefaultAsync(x => x.Id == testId);
            if (test == null || !test.IsPublished) return NotFound();

            var now = DateTime.UtcNow;
            var availIds = await _assessSvc.GetAvailableTestIdsAsync(uid, now);
            if (!availIds.Contains(testId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(scheduleId))
            {
                var ok = _examAccessTokenService.Validate(
                    accessToken ?? string.Empty,
                    uid,
                    testId,
                    scheduleId,
                    out _);
                if (!ok) return Forbid();
            }

            var fingerprint = _sessionDeviceGuardService.GetRequestFingerprint(
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            var hasOtherActiveDevice = await _sessionDeviceGuardService.HasActiveSessionOnOtherDeviceAsync(uid, fingerprint);
            if (hasOtherActiveDevice)
            {
                TempData["Err"] = "Bạn đang có phiên làm bài trên thiết bị khác. Vui lòng kết thúc phiên đó trước khi bắt đầu.";
                return RedirectToAction("Index", "MyTests");
            }

            var sessionsOfUser = (await _sRepo.GetAllAsync()).Where(s => s.UserId == uid && s.TestId == testId).ToList();
            var submittedStatuses = new[] { SessionStatus.Submitted, SessionStatus.AutoSubmitted, SessionStatus.Graded };
            var latestSubmitted = sessionsOfUser
                .Where(s => submittedStatuses.Contains(s.Status))
                .OrderByDescending(s => s.EndAt ?? s.StartAt)
                .FirstOrDefault();
            if (latestSubmitted != null)
                return Redirect($"/mytests/result/{latestSubmitted.Id}");

            var inProgress = sessionsOfUser
                .Where(s => s.Status == SessionStatus.InProgress)
                .OrderByDescending(s => s.StartAt)
                .FirstOrDefault();
            if (inProgress != null)
            {
                var sessionAllowed = await _sessionDeviceGuardService.EnsureSessionDeviceAsync(
                    inProgress.Id,
                    fingerprint,
                    Request.Headers["User-Agent"].ToString(),
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                if (!sessionAllowed)
                {
                    TempData["Err"] = "Phiên đang làm đã được gắn với thiết bị khác.";
                    return RedirectToAction("Index", "MyTests");
                }
                return Redirect($"/mytests/session/{inProgress.Id}");
            }

            var sNew = await _testSvc.StartAsync(testId, uid);
            var bound = await _sessionDeviceGuardService.EnsureSessionDeviceAsync(
                sNew.Id,
                fingerprint,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!bound)
            {
                TempData["Err"] = "Không thể bắt đầu phiên làm bài trên thiết bị này.";
                return RedirectToAction("Index", "MyTests");
            }

            return Redirect($"/mytests/session/{sNew.Id}");
        }

        [HttpGet("/mytests/session/{id}")]
        public async Task<IActionResult> Runner(string id)
        {
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            var t = s is null ? null : await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId);
            if (s == null || t == null) return NotFound();

            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (uid == null || !string.Equals(uid, s.UserId, StringComparison.Ordinal)) return Forbid();

            var requestFp = _sessionDeviceGuardService.GetRequestFingerprint(
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            var deviceOk = await _sessionDeviceGuardService.EnsureSessionDeviceAsync(
                s.Id,
                requestFp,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!deviceOk)
            {
                TempData["Err"] = "Phiên làm bài này thuộc về thiết bị khác.";
                return RedirectToAction("Index", "MyTests");
            }

            s.LastActivityAt = DateTime.UtcNow;
            if (!s.TimerStartedAt.HasValue)
                s.TimerStartedAt = DateTime.UtcNow;

            await _sRepo.UpsertAsync(x => x.Id == s.Id, s);

            ViewBag.TestTitle = t.Title;
            ViewBag.Duration = t.DurationMinutes;
            ViewBag.RemainingSeconds = ComputeRemainingSeconds(s, t);
            return View(s);
        }

        [HttpGet("/mytests/result/{id}")]
        public async Task<IActionResult> Result(string id)
        {
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (uid == null || !string.Equals(uid, s.UserId, StringComparison.Ordinal)) return Forbid();

            var currentTestId = s.TestId;
            var submittedStatuses = new[] { SessionStatus.Submitted, SessionStatus.AutoSubmitted, SessionStatus.Graded };
            await _sRepo.DeleteAsync(x => x.UserId == uid
                                          && x.TestId == currentTestId
                                          && x.Id != s.Id
                                          && !submittedStatuses.Contains(x.Status));

            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId);
            ViewBag.TestTitle = t?.Title ?? "Result";
            ViewBag.HasPendingRegrade = await _gradingService.HasPendingRegradeRequestAsync(id);
            return View(s);
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

        private int ComputeRemainingSeconds(Session s, Test t)
        {
            var total = Math.Max(1, t.DurationMinutes) * 60;
            var runningDelta = s.TimerStartedAt.HasValue
                ? (int)Math.Floor((DateTime.UtcNow - s.TimerStartedAt.Value).TotalSeconds)
                : 0;
            var consumed = Math.Max(0, s.ConsumedSeconds + runningDelta);
            return Math.Max(0, total - consumed);
        }
    }
}
