using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.ViewModels.Grading;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
    public class GradingController : Controller
    {
        private readonly IGradingService _gradingService;

        public GradingController(IGradingService gradingService)
        {
            _gradingService = gradingService;
        }

        [HttpGet("/grading/pending")]
        public async Task<IActionResult> Pending()
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var sessions = await _gradingService.GetPendingGradingSessionsAsync(lecturerId);

            var lockMap = new Dictionary<string, bool>(StringComparer.Ordinal);
            var regradeMap = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var s in sessions)
            {
                lockMap[s.Id] = await _gradingService.IsGradeLockedAsync(s.Id);
                regradeMap[s.Id] = await _gradingService.HasPendingRegradeRequestAsync(s.Id);
            }

            ViewBag.LockMap = lockMap;
            ViewBag.RegradeMap = regradeMap;
            return View(sessions);
        }

        [HttpGet("/grading/{id:length(32)}")]
        public async Task<IActionResult> Edit(string id)
        {
            var s = await _gradingService.GetSessionForGradingAsync(id);
            if (s == null) return NotFound();

            var testQPoints = s.Test?.TestQuestions.ToDictionary(tq => tq.QuestionId, tq => tq.Points) ?? new Dictionary<string, decimal>();

            var vm = new GradeSessionViewModel
            {
                Session = s,
                Test = s.Test ?? new Test(),
                Essays = s.StudentAnswers
                    .Where(sa => sa.Question != null && sa.Question.Type == QType.Essay)
                    .Select(sa => new GradeSessionViewModel.EssayItem
                    {
                        QuestionId = sa.QuestionId,
                        Content = sa.Question?.Content ?? "(Question removed)",
                        UserAnswer = sa.EssayAnswer,
                        MaxPoints = testQPoints.TryGetValue(sa.QuestionId, out var p) ? p : 1m,
                        GivenScore = sa.Score,
                        Comment = sa.Comment
                    }).ToList()
            };

            ViewBag.IsLocked = await _gradingService.IsGradeLockedAsync(id);
            ViewBag.HasPendingRegrade = await _gradingService.HasPendingRegradeRequestAsync(id);
            ViewBag.PendingRegradeReason = await _gradingService.GetPendingRegradeReasonAsync(id);
            ViewBag.ModerationLogs = await _gradingService.GetModerationLogsAsync(id);
            return View(vm);
        }

        [HttpPost("/grading/{id:length(32)}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] Dictionary<string, decimal> scores, [FromForm] Dictionary<string, string> comments, bool finalize = false)
        {
            try
            {
                foreach (var qid in scores.Keys)
                {
                    var score = scores[qid];
                    var comment = comments.ContainsKey(qid) ? comments[qid] : null;
                    await _gradingService.GradeEssayAsync(id, qid, score, comment);
                }

                if (finalize)
                {
                    await _gradingService.FinalizeGradingAsync(id);
                    TempData["Msg"] = "Grading finalized and saved.";
                    return RedirectToAction(nameof(Pending));
                }

                TempData["Msg"] = "Scores saved as draft.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Err"] = "Error saving grades: " + ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [HttpPost("/grading/{id:length(32)}/lock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id, string? note = null)
        {
            try
            {
                await _gradingService.LockGradeAsync(id, User.Identity?.Name ?? "unknown", note);
                TempData["Msg"] = "Grade is now locked.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = "Error locking grade: " + ex.Message;
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost("/grading/{id:length(32)}/unlock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id, string? note = null)
        {
            try
            {
                await _gradingService.UnlockGradeAsync(id, User.Identity?.Name ?? "unknown", note);
                TempData["Msg"] = "Grade lock removed.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = "Error unlocking grade: " + ex.Message;
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet("/grading/regrade/requests")]
        public async Task<IActionResult> RegradeRequests()
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _gradingService.GetPendingRegradeRequestsAsync(lecturerId);
            return View(items);
        }

        [HttpPost("/grading/regrade/{id:length(32)}/resolve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveRegrade(string id, bool approved, string? note = null)
        {
            try
            {
                await _gradingService.ResolveRegradeRequestAsync(id, User.Identity?.Name ?? "unknown", approved, note);
                TempData["Msg"] = approved ? "Regrade approved." : "Regrade rejected.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = "Error resolving regrade request: " + ex.Message;
            }
            return RedirectToAction(nameof(RegradeRequests));
        }
    }
}
