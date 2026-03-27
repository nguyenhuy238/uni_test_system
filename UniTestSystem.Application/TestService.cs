using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Application
{
    public class TestService
    {
        private readonly IRepository<Question> _qRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Session> _sRepo;

        public TestService(IRepository<Question> qRepo, IRepository<Test> tRepo, IRepository<Session> sRepo)
        { _qRepo = qRepo; _tRepo = tRepo; _sRepo = sRepo; }

        public async Task<Session> StartAsync(string testId, string userId)
        {
            // CHECK: Existing InProgress session
            var existingSpec = new Specification<Session>(x => x.TestId == testId && x.UserId == userId && x.Status == SessionStatus.InProgress && !x.IsDeleted)
                .Include(s => s.StudentAnswers);
            var existing = await _sRepo.FirstOrDefaultAsync(existingSpec);
            if (existing != null) return existing;

            var test = await GetTestWithQuestionsAsync(testId) ?? throw new Exception("Test not found");
            var all = await _qRepo.GetAllAsync();

            // Pick questions for this session
            var pickedQuestions = await BuildSnapshotAsync(test, all);
            var pointsPerQuestion = BuildPointsMap(test, pickedQuestions);

            var ses = new Session
            {
                TestId = testId,
                UserId = userId,
                StartAt = DateTime.UtcNow,
                Status = SessionStatus.InProgress,
                TimerStartedAt = DateTime.UtcNow,
                AutoScore = 0m,
                ManualScore = 0m,
                TotalScore = 0m,
                MaxScore = Math.Round(pointsPerQuestion.Values.Sum(), 2),
                Percent = 0m
            };

            // Initialize StudentAnswers for this session (recording the questions chosen)
            foreach (var q in pickedQuestions)
            {
                ses.StudentAnswers.Add(new StudentAnswer
                {
                    QuestionId = q.Id,
                    Score = 0,
                    AnsweredAt = DateTime.UtcNow
                });
            }

            await _sRepo.InsertAsync(ses);
            return ses;
        }

        public async Task<Session> SubmitAsync(string sessionId, Dictionary<string, string?> answers)
        {
            var sessionSpec = new Specification<Session>(x => x.Id == sessionId)
                .Include(s => s.StudentAnswers);
            var s = await _sRepo.FirstOrDefaultAsync(sessionSpec) ?? throw new Exception("Session not found");
            if (s.Status == SessionStatus.Submitted) return s;

            var test = await GetTestWithQuestionsAsync(s.TestId) ?? throw new Exception("Test not found");
            var allQuestions = await _qRepo.GetAllAsync();
            var qMap = allQuestions.ToDictionary(q => q.Id, q => q);

            decimal autoScore = 0.0m;
            decimal autoMax = 0.0m;
            decimal manualMax = 0.0m;

            // Map points
            var pointsByQ = BuildPointsMap(test, s.StudentAnswers.Select(sa => qMap[sa.QuestionId]).ToList());

            foreach (var sa in s.StudentAnswers)
            {
                if (!qMap.TryGetValue(sa.QuestionId, out var q)) continue;

                answers.TryGetValue(q.Id, out var selRaw);
                selRaw ??= "";

                decimal grade = 0.0m; // 0..1
                bool isAuto = true;

                switch (q.Type)
                {
                    case QType.MCQ:
                    case QType.TrueFalse:
                        {
                            var correct = q.Options.Where(o => o.IsCorrect).Select(o => o.Content).ToList();
                            var sel = selRaw.Trim();
                            grade = (!string.IsNullOrEmpty(sel) && correct.Any() && correct.Contains(sel)) ? 1.0m : 0.0m;
                            sa.SelectedOptionId = sel; // Reuse field for content if ID not available
                            sa.Score = grade;
                            break;
                        }
                    case QType.Essay:
                        {
                            isAuto = false;
                            sa.EssayAnswer = selRaw;
                            sa.Score = 0;
                            break;
                        }
                    case QType.Matching:
                        {
                            // Expected format: one pair per line "Left=Right" (or separated by ||).
                            var expected = (q.MatchingPairs ?? new List<MatchPair>())
                                .Where(p => !string.IsNullOrWhiteSpace(p.L))
                                .GroupBy(p => NormalizeToken(p.L), StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => NormalizeToken(g.First().R), StringComparer.OrdinalIgnoreCase);

                            var actual = ParseKeyValueAnswer(selRaw);
                            grade = CalculatePartialMatchScore(expected, actual);
                            sa.EssayAnswer = selRaw;
                            sa.Score = grade;
                            break;
                        }
                    case QType.DragDrop:
                        {
                            // Expected format: one mapping per line "Slot=Token" (or separated by ||).
                            var expected = (q.DragDrop?.Slots ?? new List<DragSlot>())
                                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                                .GroupBy(s => NormalizeToken(s.Name), StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => NormalizeToken(g.First().Answer), StringComparer.OrdinalIgnoreCase);

                            var actual = ParseKeyValueAnswer(selRaw);
                            grade = CalculatePartialMatchScore(expected, actual);
                            sa.EssayAnswer = selRaw;
                            sa.Score = grade;
                            break;
                        }
                    default:
                        // Other types logic simplified or moved to normalized structure later
                        break;
                }

                var qPoints = pointsByQ.TryGetValue(q.Id, out var p) ? p : 1.0m;

                if (isAuto)
                {
                    autoScore += grade * qPoints;
                    autoMax += qPoints;
                }
                else
                {
                    manualMax += qPoints;
                }
                sa.AnsweredAt = DateTime.UtcNow;
            }

            s.EndAt = DateTime.UtcNow;
            s.Status = SessionStatus.Submitted;

            s.AutoScore = Math.Round(autoScore, 2);
            s.TotalScore = Math.Round(s.AutoScore + s.ManualScore, 2);
            s.MaxScore = Math.Round(autoMax + manualMax, 2);
            s.Percent = s.MaxScore > 0 ? Math.Round((s.TotalScore / s.MaxScore) * 100.0m, 2) : 0m;
            s.IsPassed = s.TotalScore >= test.PassScore;

            await _sRepo.UpsertAsync(x => x.Id == s.Id, s);
            return s;
        }

        private static Dictionary<string, decimal> BuildPointsMap(Test t, IEnumerable<Question> snapshot)
        {
            var map = new Dictionary<string, decimal>();
            if (t.TestQuestions != null && t.TestQuestions.Count > 0)
            {
                var testQMap = t.TestQuestions.ToDictionary(i => i.QuestionId, i => i.Points);
                foreach (var q in snapshot)
                    map[q.Id] = testQMap.TryGetValue(q.Id, out var p) ? p : 1m;
            }
            else if (t.QuestionSnapshots != null && t.QuestionSnapshots.Count > 0)
            {
                var snapshotPointMap = t.QuestionSnapshots
                    .GroupBy(i => i.OriginalQuestionId)
                    .ToDictionary(g => g.Key, g => g.First().Points);
                foreach (var q in snapshot)
                    map[q.Id] = snapshotPointMap.TryGetValue(q.Id, out var p) ? p : 1m;
            }
            else
            {
                foreach (var q in snapshot) map[q.Id] = 1m;
            }
            return map;
        }

        private Task<List<Question>> BuildSnapshotAsync(Test t, IEnumerable<Question> all)
        {
            if (t.TestQuestions != null && t.TestQuestions.Any())
            {
                var qIds = t.TestQuestions.Select(i => i.QuestionId).ToHashSet();
                return Task.FromResult(all.Where(q => qIds.Contains(q.Id)).ToList());
            }

            if (t.QuestionSnapshots != null && t.QuestionSnapshots.Any())
            {
                var qIds = t.QuestionSnapshots
                    .OrderBy(i => i.Order)
                    .Select(i => i.OriginalQuestionId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);
                return Task.FromResult(all.Where(q => qIds.Contains(q.Id)).ToList());
            }

            return Task.FromResult(new List<Question>());
        }

        private static Dictionary<string, string> ParseKeyValueAnswer(string? raw)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var normalized = raw.Replace("||", Environment.NewLine);
            var lines = normalized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = NormalizeToken(line[..idx]);
                var value = NormalizeToken(line[(idx + 1)..]);
                if (string.IsNullOrWhiteSpace(key)) continue;
                result[key] = value;
            }

            return result;
        }

        private static decimal CalculatePartialMatchScore(
            Dictionary<string, string> expected,
            Dictionary<string, string> actual)
        {
            if (expected.Count == 0) return 0m;

            var correct = 0;
            foreach (var pair in expected)
            {
                if (actual.TryGetValue(pair.Key, out var answer) &&
                    string.Equals(answer, pair.Value, StringComparison.OrdinalIgnoreCase))
                {
                    correct++;
                }
            }

            return (decimal)correct / expected.Count;
        }

        private static string NormalizeToken(string? input)
            => (input ?? string.Empty).Trim().ToLowerInvariant();

        private async Task<Test?> GetTestWithQuestionsAsync(string testId)
        {
            var spec = new Specification<Test>(t => t.Id == testId)
                .Include(t => t.TestQuestions)
                .Include(t => t.QuestionSnapshots);
            return await _tRepo.FirstOrDefaultAsync(spec);
        }
    }
}
