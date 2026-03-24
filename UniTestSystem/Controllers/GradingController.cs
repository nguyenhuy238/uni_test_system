using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            return View(sessions);
        }

        [HttpGet("/grading/{id}")]
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
                    .Where(sa => sa.Question.Type == QType.Essay)
                    .Select(sa => new GradeSessionViewModel.EssayItem
                    {
                        QuestionId = sa.QuestionId,
                        Content = sa.Question.Content,
                        UserAnswer = sa.EssayAnswer,
                        MaxPoints = testQPoints.TryGetValue(sa.QuestionId, out var p) ? p : 1m,
                        GivenScore = sa.Score,
                        Comment = sa.Comment
                    }).ToList()
            };

            return View(vm);
        }

        [HttpPost("/grading/{id}")]
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
    }
}
