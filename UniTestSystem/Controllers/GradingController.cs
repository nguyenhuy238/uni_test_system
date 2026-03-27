using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
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

        [HttpGet("/grading")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Pending));
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

            var testQPoints = BuildQuestionPointsMap(
                s.Test,
                s.StudentAnswers.Select(sa => sa.QuestionId));
            var questionOrder = BuildQuestionOrderMap(s.Test);

            var vm = new GradeSessionViewModel
            {
                Session = s,
                Test = s.Test ?? new Test(),
                Answers = s.StudentAnswers
                    .Where(sa => sa.Question != null)
                    .OrderBy(sa => questionOrder.TryGetValue(sa.QuestionId, out var order) ? order : int.MaxValue)
                    .ThenBy(sa => sa.QuestionId, StringComparer.Ordinal)
                    .Select(sa =>
                    {
                        var q = sa.Question!;
                        var maxPoints = testQPoints.TryGetValue(sa.QuestionId, out var p) ? p : 1m;
                        if (maxPoints <= 0m) maxPoints = 1m;

                        var isAutoGradable = q.Type != QType.Essay;
                        return new GradeSessionViewModel.AnswerItem
                        {
                            QuestionId = sa.QuestionId,
                            Type = q.Type,
                            TypeLabel = GetQuestionTypeLabel(q.Type),
                            Content = q.Content ?? "(Question removed)",
                            UserAnswerDisplay = BuildUserAnswerDisplay(q, sa),
                            CorrectAnswerDisplay = BuildCorrectAnswerDisplay(q),
                            MaxPoints = maxPoints,
                            GivenScore = Math.Clamp(sa.Score, 0m, maxPoints),
                            AutoSuggestedScore = isAutoGradable && !sa.GradedAt.HasValue
                                ? Math.Clamp(sa.Score, 0m, maxPoints)
                                : null,
                            IsAutoGradable = isAutoGradable,
                            IsManuallyGraded = sa.GradedAt.HasValue,
                            Comment = sa.Comment
                        };
                    })
                    .ToList()
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
                    await _gradingService.GradeAnswerAsync(id, qid, score, comment);
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

        private static Dictionary<string, decimal> BuildQuestionPointsMap(Test? test, IEnumerable<string>? questionIds)
        {
            var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var orderedQuestionIds = (questionIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (test == null)
            {
                return TestScoreDistribution.AllocateEvenlyByQuestionIds(
                    orderedQuestionIds,
                    TestScoreDistribution.FixedTotalScore);
            }

            if (test.TestQuestions != null && test.TestQuestions.Count > 0)
            {
                foreach (var item in test.TestQuestions)
                {
                    if (string.IsNullOrWhiteSpace(item.QuestionId)) continue;
                    map[item.QuestionId] = item.Points > 0m ? item.Points : 1m;
                }
            }

            if (test.QuestionSnapshots != null && test.QuestionSnapshots.Count > 0)
            {
                foreach (var item in test.QuestionSnapshots)
                {
                    if (string.IsNullOrWhiteSpace(item.OriginalQuestionId)) continue;
                    map.TryAdd(item.OriginalQuestionId, item.Points > 0m ? item.Points : 1m);
                }
            }

            if (orderedQuestionIds.Count == 0)
            {
                orderedQuestionIds = map.Keys.ToList();
            }

            return TestScoreDistribution.NormalizeOrAllocate(
                orderedQuestionIds,
                map,
                TestScoreDistribution.FixedTotalScore);
        }

        private static Dictionary<string, int> BuildQuestionOrderMap(Test? test)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            if (test == null)
            {
                return map;
            }

            if (test.TestQuestions != null && test.TestQuestions.Count > 0)
            {
                foreach (var item in test.TestQuestions.OrderBy(x => x.Order))
                {
                    if (string.IsNullOrWhiteSpace(item.QuestionId) || map.ContainsKey(item.QuestionId)) continue;
                    map[item.QuestionId] = item.Order;
                }
            }

            if (map.Count == 0 && test.QuestionSnapshots != null && test.QuestionSnapshots.Count > 0)
            {
                foreach (var item in test.QuestionSnapshots.OrderBy(x => x.Order))
                {
                    if (string.IsNullOrWhiteSpace(item.OriginalQuestionId) || map.ContainsKey(item.OriginalQuestionId)) continue;
                    map[item.OriginalQuestionId] = item.Order;
                }
            }

            return map;
        }

        private static string GetQuestionTypeLabel(QType type) => type switch
        {
            QType.MCQ => "MCQ",
            QType.TrueFalse => "True/False",
            QType.Essay => "Essay",
            QType.Matching => "Matching",
            QType.DragDrop => "DragDrop",
            _ => type.ToString()
        };

        private static string BuildUserAnswerDisplay(Question question, StudentAnswer answer)
        {
            if (question.Type == QType.Essay)
            {
                return string.IsNullOrWhiteSpace(answer.EssayAnswer) ? "(chua tra loi)" : answer.EssayAnswer.Trim();
            }

            if (question.Type == QType.MCQ || question.Type == QType.TrueFalse)
            {
                var selectedRaw = answer.SelectedOptionId?.Trim();
                if (string.IsNullOrWhiteSpace(selectedRaw))
                {
                    return "(chua chon)";
                }

                var orderedOptions = question.Options
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Id, StringComparer.Ordinal)
                    .ToList();

                var selectedOption = orderedOptions.FirstOrDefault(o =>
                    string.Equals(o.Id, selectedRaw, StringComparison.OrdinalIgnoreCase));
                if (selectedOption == null && selectedRaw.Length == 1 && char.IsLetter(selectedRaw[0]))
                {
                    var idx = char.ToUpperInvariant(selectedRaw[0]) - 'A';
                    if (idx >= 0 && idx < orderedOptions.Count)
                    {
                        selectedOption = orderedOptions[idx];
                    }
                }

                if (selectedOption == null)
                {
                    selectedOption = orderedOptions.FirstOrDefault(o =>
                        string.Equals(o.Content?.Trim(), selectedRaw, StringComparison.OrdinalIgnoreCase));
                }

                if (selectedOption == null)
                {
                    return selectedRaw;
                }

                if (question.Type == QType.MCQ)
                {
                    var selectedIndex = orderedOptions.FindIndex(o => o.Id == selectedOption.Id);
                    var selectedLabel = selectedIndex >= 0 ? ((char)('A' + selectedIndex)).ToString() : "";
                    return $"{selectedLabel}) {selectedOption.Content}";
                }

                return selectedOption.Content;
            }

            var mapping = ParseKeyValueAnswer(answer.EssayAnswer ?? answer.SelectedOptionId);
            if (mapping.Count == 0)
            {
                return "(chua tra loi)";
            }

            return string.Join(Environment.NewLine, mapping.Select(kv => $"{kv.Key} -> {kv.Value}"));
        }

        private static string BuildCorrectAnswerDisplay(Question question)
        {
            if (question.Type == QType.Essay)
            {
                return "(cham tay)";
            }

            if (question.Type == QType.MCQ || question.Type == QType.TrueFalse)
            {
                var orderedOptions = question.Options
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Id, StringComparer.Ordinal)
                    .ToList();

                var correctParts = new List<string>();
                for (var i = 0; i < orderedOptions.Count; i++)
                {
                    var option = orderedOptions[i];
                    if (!option.IsCorrect) continue;
                    if (question.Type == QType.MCQ)
                    {
                        correctParts.Add($"{(char)('A' + i)}) {option.Content}");
                    }
                    else
                    {
                        correctParts.Add(option.Content);
                    }
                }

                return correctParts.Count > 0 ? string.Join(Environment.NewLine, correctParts) : "(khong xac dinh)";
            }

            if (question.Type == QType.Matching)
            {
                var pairs = question.MatchingPairs ?? new List<MatchPair>();
                if (pairs.Count == 0) return "(khong xac dinh)";
                return string.Join(Environment.NewLine, pairs.Select(p => $"{p.L} -> {p.R}"));
            }

            if (question.Type == QType.DragDrop)
            {
                var slots = question.DragDrop?.Slots ?? new List<DragSlot>();
                if (slots.Count == 0) return "(khong xac dinh)";
                return string.Join(Environment.NewLine, slots.Select(s => $"{s.Name} -> {s.Answer}"));
            }

            return "(khong xac dinh)";
        }

        private static Dictionary<string, string> ParseKeyValueAnswer(string? raw)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }

            var normalized = raw.Replace("||", Environment.NewLine, StringComparison.Ordinal);
            var lines = normalized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                result[key] = value;
            }

            return result;
        }
    }
}
