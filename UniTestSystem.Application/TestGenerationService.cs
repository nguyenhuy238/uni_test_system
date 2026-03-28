using System;
using System.Collections.Generic;
using System.Linq;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Application
{
    public class TestGenerationService : ITestGenerationService
    {
        private readonly IRepository<Question> _qRepo;
        private readonly IRepository<QuestionBank> _questionBankRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Student> _sRepo;
        private readonly IRepository<Assessment> _asRepo;

        public TestGenerationService(
            IRepository<Question> qRepo,
            IRepository<QuestionBank> questionBankRepo,
            IRepository<Test> tRepo,
            IRepository<Student> sRepo,
            IRepository<Assessment> asRepo)
        {
            _qRepo = qRepo; _questionBankRepo = questionBankRepo; _tRepo = tRepo; _sRepo = sRepo; _asRepo = asRepo;
        }

        // === Generate 1 đề chung cho nhóm ===
        public async System.Threading.Tasks.Task<(Test test, List<TestItem> items, List<Student> targets)> GenerateAsync(
            AutoTestOptions opt, string actorUserName)
        {
            var allStudents = await _sRepo.GetAllAsync();
            var targets = ResolveTargets(allStudents, opt);
            if (targets.Count == 0) throw new InvalidOperationException("Không có người dùng mục tiêu.");

            var subjects = (opt.Subjects?.Any() == true)
                ? opt.Subjects!
                : targets.GroupBy(u => (u.Major ?? "").Trim())
                         .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                         .OrderByDescending(g => g.Count()).Take(3)
                         .Select(g => g.Key).ToList();

            var majorityLevel = targets.GroupBy(u => u.AcademicYear ?? "2024")
                                       .OrderByDescending(g => g.Count())
                                       .First().Key;

            var allQs = await _qRepo.GetAllAsync();
            var coursePool = await FilterByCourseAsync(allQs, opt.CourseId);
            var pool = FilterBySubjects(coursePool, subjects);
            pool = FilterByTags(pool, opt.Tags);

            if (string.Equals(opt.DifficultyPolicy, "ByYear", StringComparison.OrdinalIgnoreCase))
                pool = FilterByDifficulty(pool, majorityLevel);

            var (picked, missing) = PickWithFallback(pool, coursePool, subjects, opt);
            if (missing > 0 && opt.FailWhenInsufficient)
                throw BuildInsufficientError(missing, opt, coursePool, subjects, pool);

            var alloc = ScoringAllocator.Allocate(
                picked.Select(p => (p.Id, p.Type.ToString(), (string?)p.DifficultyLevelId)),
                opt.TotalScore, opt.EssayReserved, opt.EssayCount);

            var map = alloc.ToDictionary(x => x.questionId, x => x.points);
            var items = picked.Select(q => new TestItem { QuestionId = q.Id, Points = map[q.Id] }).ToList();

            var test = new Test
            {
                Title = $"Auto - {string.Join(",", subjects)} - Year:{majorityLevel}",
                DurationMinutes = Math.Max(10, picked.Count * 2),
                PassScore = (int)Math.Round((double)opt.TotalScore / 2.0, MidpointRounding.AwayFromZero),
                ShuffleQuestions = true,
                TotalMaxScore = opt.TotalScore,
                CourseId = string.IsNullOrWhiteSpace(opt.CourseId) ? null : opt.CourseId.Trim(),
                TestQuestions = picked.Select((q, idx) => new TestQuestion 
                { 
                    QuestionId = q.Id, 
                    Points = map[q.Id],
                    Order = idx 
                }).ToList(),
                IsPublished = false,
                CreatedBy = actorUserName,
                CreatedAt = DateTime.UtcNow
            };
            await _tRepo.InsertAsync(test);
            return (test, items, targets);
        }

        // === Generate cá nhân hóa — mỗi student 1 Test (DRAFT) ===
        public async System.Threading.Tasks.Task<List<PersonalizedTestResult>> GeneratePersonalizedAsync(
            AutoTestOptions opt, string actorUserName)
        {
            var allStudents = await _sRepo.GetAllAsync();
            var targets = ResolveTargets(allStudents, opt);
            if (targets.Count == 0) throw new InvalidOperationException("Không có sinh viên mục tiêu.");

            var allQuestions = await _qRepo.GetAllAsync();
            var coursePool = await FilterByCourseAsync(allQuestions, opt.CourseId);
            var results = new List<PersonalizedTestResult>();

            foreach (var u in targets)
            {
                var subjectSet = (opt.Subjects?.Any() == true)
                    ? new HashSet<string>(opt.Subjects!, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(new[] { u.Major ?? "" }, StringComparer.OrdinalIgnoreCase);

                var basePool = FilterBySubjects(coursePool, subjectSet.Where(s => !string.IsNullOrWhiteSpace(s)).ToList());
                basePool = FilterByTags(basePool, opt.Tags);

                if (string.Equals(opt.DifficultyPolicy, "ByYear", StringComparison.OrdinalIgnoreCase))
                    basePool = FilterByDifficulty(basePool, u.AcademicYear ?? "2024");

                var (picked, missing) = PickWithFallback(basePool, coursePool, subjectSet.ToList(), opt);
                if (missing > 0 && opt.FailWhenInsufficient)
                    throw BuildInsufficientError(missing, opt, coursePool, subjectSet.ToList(), basePool, u);

                var alloc = ScoringAllocator.Allocate(
                    picked.Select(p => (p.Id, p.Type.ToString(), (string?)p.DifficultyLevelId)),
                    opt.TotalScore, opt.EssayReserved, opt.EssayCount);

                var map = alloc.ToDictionary(x => x.questionId, x => x.points);
                var items = picked.Select(q => new TestItem { QuestionId = q.Id, Points = map[q.Id] }).ToList();

                var test = new Test
                {
                    Title = $"Auto - {u.Name} - {u.Major} - {u.AcademicYear}",
                    DurationMinutes = Math.Max(10, picked.Count * 2),
                    PassScore = (int)Math.Round((double)opt.TotalScore / 2.0, MidpointRounding.AwayFromZero),
                    ShuffleQuestions = true,
                    TotalMaxScore = opt.TotalScore,
                    CourseId = string.IsNullOrWhiteSpace(opt.CourseId) ? null : opt.CourseId.Trim(),
                    TestQuestions = picked.Select((q, idx) => new TestQuestion 
                    { 
                        QuestionId = q.Id, 
                        Points = map[q.Id],
                        Order = idx 
                    }).ToList(),
                    IsPublished = false,
                    CreatedBy = actorUserName,
                    CreatedAt = DateTime.UtcNow
                };
                await _tRepo.InsertAsync(test);

                results.Add(new PersonalizedTestResult { User = u, Test = test, Items = items });
            }

            return results;
        }

        // === Generate + assign ngay ===
        public async System.Threading.Tasks.Task<(Test test, List<TestItem> items, List<Student> assignedUsers)> GenerateAndAssignAsync(
            AutoTestOptions opt, string actorUserName)
        {
            var (test, items, users) = await GenerateAsync(opt, actorUserName);

            var assessment = new Assessment
            {
                Title = test.Title,
                Type = AssessmentType.Quiz, // Default
                StartTime = opt.StartAtUtc ?? DateTime.UtcNow.AddDays(-1),
                EndTime = opt.EndAtUtc ?? DateTime.UtcNow.AddDays(30),
                CourseId = test.CourseId ?? "default",
                Weight = 10,
                TargetType = string.Equals(opt.Mode, "Students", StringComparison.OrdinalIgnoreCase) ? "Students" : "Class",
                TargetValue = string.Equals(opt.Mode, "Students", StringComparison.OrdinalIgnoreCase) ? string.Join(",", users.Select(u => u.Id)) : opt.StudentClassId ?? ""
            };
            await _asRepo.InsertAsync(assessment);

            test.AssessmentId = assessment.Id;
            test.IsPublished = true;
            test.PublishedAt = DateTime.UtcNow;
            await _tRepo.UpsertAsync(x => x.Id == test.Id, test);

            return (test, items, users);
        }

        // ----------------- Helpers -----------------
        private static List<Student> ResolveTargets(List<Student> all, AutoTestOptions opt)
        {
            if (string.Equals(opt.Mode, "Students", StringComparison.OrdinalIgnoreCase))
            {
                var set = new HashSet<string>(opt.UserIds ?? new(), StringComparer.Ordinal);
                return all.Where(u => set.Contains(u.Id)).ToList();
            }
            else
            {
                var classId = opt.StudentClassId ?? "";
                return all.Where(u => string.Equals(u.StudentClassId ?? "", classId, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        private static List<Question> FilterBySubjects(List<Question> pool, List<string> subjects)
        {
            if (subjects == null || subjects.Count == 0) return pool;
            var set = new HashSet<string>(subjects, StringComparer.OrdinalIgnoreCase);
            return pool.Where(q => set.Contains(q.SubjectId)).ToList();
        }

        private static List<Question> FilterByDifficulty(List<Question> pool, string academicYear)
        {
            var yearLevel = ParseAcademicYearLevel(academicYear);
            if (yearLevel >= 4)
            {
                return pool;
            }

            bool Allowed(string qDiff) => NormalizeDifficulty(qDiff) switch
            {
                "easy" => yearLevel == 1 || yearLevel == 2,
                "medium" => yearLevel == 2 || yearLevel == 3,
                "hard" => yearLevel == 3,
                _ => false
            };

            return pool.Where(q => Allowed(q.DifficultyLevelId ?? string.Empty)).ToList();
        }

        private static List<Question> FilterByTags(List<Question> pool, List<string>? tags)
        {
            if (tags == null || tags.Count == 0) return pool;
            var set = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
            return pool.Where(q => q.Tags != null && q.Tags.Any(t => set.Contains(t))).ToList();
        }

        private async System.Threading.Tasks.Task<List<Question>> FilterByCourseAsync(List<Question> pool, string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
            {
                return pool;
            }

            var normalizedCourseId = courseId.Trim();
            var bankIds = (await _questionBankRepo.GetAllAsync(qb => !qb.IsDeleted && qb.CourseId == normalizedCourseId))
                .Select(qb => qb.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);

            if (bankIds.Count == 0)
            {
                return new List<Question>();
            }

            return pool
                .Where(q => !string.IsNullOrWhiteSpace(q.QuestionBankId) && bankIds.Contains(q.QuestionBankId!))
                .ToList();
        }

        private static string NormalizeDifficulty(string difficulty)
        {
            return (difficulty ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static int ParseAcademicYearLevel(string academicYear)
        {
            if (string.IsNullOrWhiteSpace(academicYear))
            {
                return 4;
            }

            var normalized = academicYear.Trim().ToLowerInvariant();
            var compact = normalized.Replace(" ", string.Empty);
            if (compact.Length == 1 && int.TryParse(compact, out var yearDigit) && yearDigit is >= 1 and <= 4)
            {
                return yearDigit;
            }

            static int ParseToken(string value, string token)
            {
                var index = value.IndexOf(token, StringComparison.Ordinal);
                if (index < 0)
                {
                    return 0;
                }

                var yearIndex = index + token.Length;
                if (yearIndex >= value.Length)
                {
                    return 0;
                }

                var yearChar = value[yearIndex];
                if (yearChar is < '1' or > '4')
                {
                    return 0;
                }

                if (yearIndex + 1 < value.Length && char.IsDigit(value[yearIndex + 1]))
                {
                    return 0;
                }

                return yearChar - '0';
            }

            var parsedYear = ParseToken(compact, "year");
            if (parsedYear == 0) parsedYear = ParseToken(compact, "năm");
            if (parsedYear == 0) parsedYear = ParseToken(compact, "nam");
            if (parsedYear is >= 1 and <= 4) return parsedYear;

            return 4;
        }

        /// <summary>
        /// Pick đủ số câu theo từng loại; nếu thiếu → nới lỏng:
        /// (1) đúng Subject, bỏ phân loại; (2) bỏ luôn Subject. Trả về (picked, missing).
        /// </summary>
        private static (List<Question> picked, int missing) PickWithFallback(
            List<Question> initialPool,
            List<Question> allQuestions,
            List<string> subjects,
            AutoTestOptions opt)
        {
            var rnd = new Random();
            var result = new List<Question>();

            int needMCQ = Math.Max(0, opt.McqCount);
            int needTF = Math.Max(0, opt.TfCount);
            int needMT = Math.Max(0, opt.MatchingCount);
            int needDD = Math.Max(0, opt.DragDropCount);
            int needES = Math.Max(0, opt.EssayCount);
            int needTotal = needMCQ + needTF + needMT + needDD + needES;

            if (string.Equals(opt.DifficultyPolicy, "Matrix", StringComparison.OrdinalIgnoreCase) && opt.DifficultyMatrix?.Any() == true)
            {
                // Matrix mode: Pick exactly by difficulty distribution
                foreach (var dm in opt.DifficultyMatrix)
                {
                    var diffPool = initialPool.Where(q => string.Equals(q.DifficultyLevelId, dm.DifficultyLevelId, StringComparison.OrdinalIgnoreCase)).ToList();
                    var pickedForDiff = diffPool.OrderBy(_ => rnd.Next()).Take(dm.Count).ToList();
                    result.AddRange(pickedForDiff);
                }
                // If total picked is still less than needTotal, we might need to fill up or just accept it.
                // But usually Matrix mode defines the TOTAL count.
            }
            else
            {
                // Standard mode: Pick by type
                List<Question> P(IEnumerable<Question> pool, Func<Question, bool> pred, int n)
                    => pool.Where(pred).OrderBy(_ => rnd.Next()).Take(Math.Max(0, n)).ToList();

                result.AddRange(P(initialPool, q => q.Type == QType.MCQ, needMCQ));
                result.AddRange(P(initialPool, q => q.Type == QType.TrueFalse, needTF));
                result.AddRange(P(initialPool, q => q.Type == QType.Matching, needMT));
                result.AddRange(P(initialPool, q => q.Type == QType.DragDrop, needDD));
                result.AddRange(P(initialPool, q => q.Type == QType.Essay, needES));
            }

            result = result.GroupBy(q => q.Id).Select(g => g.First()).ToList();

            int missing = needTotal - result.Count;
            if (missing <= 0) return (result, 0);

            // Nới lỏng 1 — giữ Subject, bỏ phân loại/độ khó
            var subjectSet = new HashSet<string>(subjects ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            var poolBySubjectOnly = allQuestions
                .Where(q => subjectSet.Contains(q.SubjectId))
                .Where(q => !result.Any(r => r.Id == q.Id))
                .OrderBy(_ => rnd.Next())
                .Take(missing)
                .ToList();

            result.AddRange(poolBySubjectOnly);
            result = result.GroupBy(q => q.Id).Select(g => g.First()).ToList();

            missing = needTotal - result.Count;
            if (missing <= 0) return (result, 0);

            // Nới lỏng 2 — bỏ luôn Subject (toàn bộ kho)
            var poolAny = allQuestions
                .Where(q => !result.Any(r => r.Id == q.Id))
                .OrderBy(_ => rnd.Next())
                .Take(missing)
                .ToList();

            result.AddRange(poolAny);
            result = result.GroupBy(q => q.Id).Select(g => g.First()).ToList();

            missing = needTotal - result.Count;
            return (result, Math.Max(0, missing));
        }

        /// <summary>Tạo lỗi "không đủ câu" kèm chẩn đoán ngắn gọn cho UI.</summary>
        private static InvalidOperationException BuildInsufficientError(
            int missing,
            AutoTestOptions opt,
            List<Question> allQuestions,
            List<string> subjects,
            List<Question> initialPool,
            Student? u = null)
        {
            int needTotal = Math.Max(0, opt.McqCount) + Math.Max(0, opt.TfCount)
                          + Math.Max(0, opt.MatchingCount) + Math.Max(0, opt.DragDropCount)
                          + Math.Max(0, opt.EssayCount);

            var subjectSet = new HashSet<string>(subjects ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            int totalAll = allQuestions.Select(q => q.Id).Distinct().Count();
            int totalBySubject = allQuestions.Where(q => subjectSet.Contains(q.SubjectId))
                                           .Select(q => q.Id).Distinct().Count();
            int totalInitial = initialPool.Select(q => q.Id).Distinct().Count();

            string who = u == null ? "" : $"Sinh viên: {u.Name} ({u.Major}/{u.AcademicYear}). ";
            string tip = "Gợi ý: thêm câu hỏi vào kho; bổ sung Subjects; chọn DifficultyPolicy = Any; hoặc giảm số lượng yêu cầu.";
            string msg = $"{who}Không đủ câu hỏi để tạo đề: yêu cầu {needTotal} nhưng hiện chỉ có {needTotal - missing}. " +
                         $"(Kho: All={totalAll}, Theo Subject={totalBySubject}, Pool ban đầu={totalInitial}). {tip}";
            return new InvalidOperationException(msg);
        }
    }
}
