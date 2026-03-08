using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
    public class GradingController : Controller
    {
        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Question> _qRepo;

        public GradingController(IRepository<Session> s, IRepository<Test> t, IRepository<Question> q)
        { _sRepo = s; _tRepo = t; _qRepo = q; }

        [HttpGet("/grading/pending")]
        public async Task<IActionResult> Pending()
        {
            var sessions = await _sRepo.GetAllAsync();
            var questions = await _qRepo.GetAllAsync();
            var essayIds = questions.Where(q => q.Type == QType.Essay).Select(q => q.Id).ToHashSet();

            var list = sessions
                .Where(s => s.Status == SessionStatus.Submitted || s.Status == SessionStatus.Graded)
                .Where(s => s.StudentAnswers.Any(sa => essayIds.Contains(sa.QuestionId)))
                .OrderByDescending(s => s.EndAt ?? s.StartAt)
                .ToList();
            return View(list);
        }

        [HttpGet("/grading/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId) ?? new Test();
            var questions = await _qRepo.GetAllAsync();
            var qMap = questions.ToDictionary(q => q.Id, q => q);

            var testQPoints = t.TestQuestions.ToDictionary(tq => tq.QuestionId, tq => tq.Points);

            var vm = new GradeSessionViewModel
            {
                Session = s,
                Test = t,
                Essays = s.StudentAnswers
                    .Where(sa => qMap.TryGetValue(sa.QuestionId, out var q) && q.Type == QType.Essay)
                    .Select(sa =>
                    {
                        var q = qMap[sa.QuestionId];
                        return new GradeSessionViewModel.EssayItem
                        {
                            QuestionId = sa.QuestionId,
                            Content = q.Content,
                            UserAnswer = sa.EssayAnswer,
                            MaxPoints = testQPoints.TryGetValue(sa.QuestionId, out var p) ? p : 1m,
                            GivenScore = (double?)sa.Score
                        };
                    }).ToList()
            };

            return View(vm);
        }

        [HttpPost("/grading/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [FromForm] Dictionary<string, decimal> scores)
        {
            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == s.TestId) ?? new Test();
            var testQPoints = t.TestQuestions.ToDictionary(tq => tq.QuestionId, tq => tq.Points);

            // Update manual scores in StudentAnswers
            foreach (var kv in scores)
            {
                var qid = kv.Key;
                var valRaw = kv.Value;
                var max = testQPoints.TryGetValue(qid, out var p) ? p : 1m;
                var val = Math.Max(0m, Math.Min(max, valRaw));

                var sa = s.StudentAnswers.FirstOrDefault(x => x.QuestionId == qid);
                if (sa != null)
                {
                    sa.Score = val;
                }
            }

            // Recalculate totals
            var questions = await _qRepo.GetAllAsync();
            var essayIds = questions.Where(q => q.Type == QType.Essay).Select(q => q.Id).ToHashSet();

            s.ManualScore = Math.Round(s.StudentAnswers.Where(sa => essayIds.Contains(sa.QuestionId)).Sum(sa => sa.Score), 2);
            s.TotalScore = Math.Round(s.AutoScore + s.ManualScore, 2);
            s.MaxScore = Math.Round(s.StudentAnswers.Sum(sa => testQPoints.TryGetValue(sa.QuestionId, out var p) ? p : 1m), 2);
            s.Percent = s.MaxScore > 0 ? Math.Round(s.TotalScore * 100.0m / s.MaxScore, 2) : 0m;
            s.IsPassed = s.TotalScore >= t.PassScore;

            s.Status = SessionStatus.Graded;
            s.LastActivityAt = DateTime.UtcNow;

            await _sRepo.UpsertAsync(x => x.Id == s.Id, s);

            TempData["Msg"] = "Đã lưu điểm Essay.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }
}
