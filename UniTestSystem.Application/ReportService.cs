using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application
{
    public class ReportService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Test> _testRepo;
        private readonly IRepository<Assessment> _asRepo;
        private readonly IRepository<Session> _sesRepo;
        private readonly IRepository<Question> _qRepo;
        private readonly IRepository<Enrollment> _enrollmentRepo;

        public ReportService(
            IRepository<User> u, 
            IRepository<Student> st,
            IRepository<Test> t, 
            IRepository<Assessment> a, 
            IRepository<Session> s,
            IRepository<Question> q,
            IRepository<Enrollment> enrollmentRepo)
        {
            _userRepo = u; _studentRepo = st; _testRepo = t; _asRepo = a; _sesRepo = s; _qRepo = q; _enrollmentRepo = enrollmentRepo;
        }

        // ==== Báo cáo theo Khoa (Faculty) ====
        public async Task<FacultyReportVm> GetFacultyReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            var passMap = (await _testRepo.GetAllAsync())
                .ToDictionary(x => x.Id, x => (decimal)x.PassScore);

            var sessions = await _sesRepo.GetAllAsync(s =>
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted);

            var students = await _studentRepo.GetAllAsync();
            var studentClassMap = students.ToDictionary(st => st.Id, st => st.StudentClassId ?? "(Unknown)");

            var data = sessions
                .Where(s => studentClassMap.ContainsKey(s.UserId))
                .Select(s => new
                {
                    FacultyName = studentClassMap[s.UserId],
                    StudentId = s.UserId,
                    s.TestId,
                    s.TotalScore,
                    s.EndAt
                })
                .ToList();

            var rows = data
                .GroupBy(x => x.FacultyName)
                .Select(g =>
                {
                    var passCount = g.Count(x => passMap.TryGetValue(x.TestId, out var pass) && x.TotalScore >= pass);
                    var submitCount = g.Count();
                    return new FacultyReportRow
                    {
                        FacultyName = g.Key,
                        StudentCount = g.Select(x => x.StudentId).Distinct().Count(),
                        SubmissionCount = submitCount,
                        AvgScore = submitCount > 0 ? Math.Round(g.Average(x => x.TotalScore), 2) : 0m,
                        PassRatePercent = submitCount > 0 ? Math.Round((passCount * 100m) / submitCount, 2) : 0m,
                        LastSubmissionAt = g.Max(x => x.EndAt)
                    };
                })
                .OrderBy(x => x.FacultyName)
                .ToList();

            return new FacultyReportVm { Rows = rows };
        }

        // ==== Báo cáo theo Năm học (Academic Year) ====
        public async Task<AcademicYearReportVm> GetAcademicYearReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            var passMap = (await _testRepo.GetAllAsync())
                .ToDictionary(x => x.Id, x => (decimal)x.PassScore);

            var sessions = await _sesRepo.GetAllAsync(s =>
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted);

            var students = await _studentRepo.GetAllAsync();
            var studentYearMap = students.ToDictionary(st => st.Id, st => st.AcademicYear ?? "(Unknown)");

            var data = sessions
                .Where(s => studentYearMap.ContainsKey(s.UserId))
                .Select(s => new
                {
                    AcademicYear = studentYearMap[s.UserId],
                    StudentId = s.UserId,
                    s.TestId,
                    s.TotalScore,
                    s.EndAt
                })
                .ToList();

            var rows = data
                .GroupBy(x => x.AcademicYear)
                .Select(g =>
                {
                    var passCount = g.Count(x => passMap.TryGetValue(x.TestId, out var pass) && x.TotalScore >= pass);
                    var submitCount = g.Count();
                    return new AcademicYearReportRow
                    {
                        AcademicYear = g.Key,
                        StudentCount = g.Select(x => x.StudentId).Distinct().Count(),
                        SubmissionCount = submitCount,
                        AvgScore = submitCount > 0 ? Math.Round(g.Average(x => x.TotalScore), 2) : 0m,
                        PassRatePercent = submitCount > 0 ? Math.Round((passCount * 100m) / submitCount, 2) : 0m,
                        LastSubmissionAt = g.Max(x => x.EndAt)
                    };
                })
                .OrderBy(x => x.AcademicYear)
                .ToList();

            return new AcademicYearReportVm { Rows = rows };
        }

        // ==== Báo cáo kết quả theo môn học của Sinh viên ====
        public async Task<StudentSubjectReportVm> GetStudentSubjectReportAsync(string userId, DateTime fromUtc, DateTime toUtc)
        {
            // Optimized query: filter by userId and date range at DB level
            var spec = new Specification<Session>(s =>
                    s.UserId == userId &&
                    s.EndAt.HasValue &&
                    s.EndAt.Value >= fromUtc &&
                    s.EndAt.Value <= toUtc &&
                    s.Status != SessionStatus.NotStarted)
                .Include(s => s.StudentAnswers!);
            var sessions = await _sesRepo.ListAsync(spec);

            var subjectData = new Dictionary<string, (int QCount, decimal TScore, DateTime? LastSub)>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in sessions)
            {
                foreach (var ans in s.StudentAnswers)
                {
                    // For now, using a general group since joined Question-Subject fetching is complex in this generic repo
                    var subj = "General Academic"; 
                    
                    if (!subjectData.TryGetValue(subj, out var data)) data = (0, 0m, null);
                    data.QCount++;
                    data.TScore += ans.Score;
                    if (data.LastSub == null || s.EndAt > data.LastSub) data.LastSub = s.EndAt;
                    subjectData[subj] = data;
                }
            }

            var rows = subjectData.Select(kvp => new StudentSubjectReportRow
            {
                Subject = kvp.Key,
                QuestionCount = kvp.Value.QCount,
                TotalScore = kvp.Value.TScore,
                AvgPerQuestion = kvp.Value.QCount > 0 ? Math.Round(kvp.Value.TScore / kvp.Value.QCount, 2) : 0m,
                LastSubmissionAt = kvp.Value.LastSub
            }).OrderBy(r => r.Subject).ToList();

            return new StudentSubjectReportVm { Rows = rows };
        }

        // CSV nhanh
        public async Task<(string fileName, string csv)> ExportRecentSubmissionsCsvAsync()
        {
            var now = DateTime.UtcNow;
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();
            var sessions = await _sesRepo.GetAllAsync();

            var submitted = sessions.Where(s => s.Status != SessionStatus.NotStarted && s.EndAt != null)
                                    .OrderByDescending(s => s.EndAt)
                                    .Take(100).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SessionId,Student,Test,Score,IsPass,EndAt");

            var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);

            foreach (var s in submitted)
            {
                var uname = users.FirstOrDefault(u => u.Id == s.UserId)?.Name ?? s.UserId;
                var ttitle = tests.FirstOrDefault(t => t.Id == s.TestId)?.Title ?? s.TestId;
                var isPass = passMap.TryGetValue(s.TestId, out var p) && s.TotalScore >= (decimal)p;
                sb.AppendLine($"{s.Id},\"{uname.Replace("\"", "\"\"")}\",\"{ttitle.Replace("\"", "\"\"")}\",{s.TotalScore},{isPass},{s.EndAt:O}");
            }
            return ($"recent-submissions-{now:yyyyMMddHHmmss}.csv", sb.ToString());
        }

        // ==== Widget Dashboard (Lecturer/Staff/Admin) ====
        public async Task<WidgetDashboardVm> GetWidgetDashboardAsync(DateTime fromUtc, DateTime toUtc, Role actorRole, string? actorUserId)
        {
            var spec = new Specification<Session>(s =>
                    s.EndAt.HasValue &&
                    s.EndAt.Value >= fromUtc &&
                    s.EndAt.Value <= toUtc &&
                    s.Status != SessionStatus.NotStarted)
                .Include("Test.Course");

            var sessions = await _sesRepo.ListAsync(spec);
            if (actorRole == Role.Lecturer && !string.IsNullOrWhiteSpace(actorUserId))
            {
                sessions = sessions
                    .Where(s => s.Test?.Course?.LecturerId == actorUserId)
                    .ToList();
            }

            if (!sessions.Any())
                return new WidgetDashboardVm();

            var enrollmentSemesterMap = (await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted && !string.IsNullOrWhiteSpace(e.Semester)))
                .Select(e => new { e.StudentId, e.CourseId, e.Semester })
                .ToDictionary(
                    x => $"{x.StudentId}|{x.CourseId}",
                    x => x.Semester ?? "",
                    StringComparer.OrdinalIgnoreCase);

            var points = sessions.Select(s =>
            {
                var normalizedScore = NormalizeScoreTo10(s);
                var passScore = s.Test?.PassScore ?? 5;
                var isPass = s.TotalScore >= passScore;
                var subject = ResolveSubjectLabel(s);
                var semester = ResolveSemesterLabel(s, enrollmentSemesterMap);
                return new
                {
                    Score = normalizedScore,
                    IsPass = isPass,
                    Subject = subject,
                    Semester = semester
                };
            }).ToList();

            var subjectRows = points
                .GroupBy(x => x.Subject)
                .Select(g =>
                {
                    var submissionCount = g.Count();
                    var passCount = g.Count(x => x.IsPass);
                    var failCount = submissionCount - passCount;
                    return new SubjectPassRateRow
                    {
                        Subject = g.Key,
                        SubmissionCount = submissionCount,
                        PassCount = passCount,
                        FailCount = failCount,
                        PassRatePercent = submissionCount > 0 ? Math.Round(passCount * 100m / submissionCount, 2) : 0m,
                        AvgScore = submissionCount > 0 ? Math.Round(g.Average(x => x.Score), 2) : 0m
                    };
                })
                .OrderByDescending(x => x.SubmissionCount)
                .ThenBy(x => x.Subject)
                .ToList();

            var semesterRows = points
                .GroupBy(x => x.Semester)
                .Select(g => new SemesterAverageRow
                {
                    Semester = g.Key,
                    SubmissionCount = g.Count(),
                    AvgScore = Math.Round(g.Average(x => x.Score), 2)
                })
                .OrderBy(x => x.Semester)
                .ToList();

            var ranges = new[]
            {
                (Label: "0-2", Min: 0m, Max: 2m, IncludeMax: false),
                (Label: "2-4", Min: 2m, Max: 4m, IncludeMax: false),
                (Label: "4-6", Min: 4m, Max: 6m, IncludeMax: false),
                (Label: "6-8", Min: 6m, Max: 8m, IncludeMax: false),
                (Label: "8-10", Min: 8m, Max: 10m, IncludeMax: true)
            };

            var total = points.Count;
            var distRows = ranges.Select(r =>
            {
                var count = points.Count(x => x.Score >= r.Min && (r.IncludeMax ? x.Score <= r.Max : x.Score < r.Max));
                return new ScoreDistributionBucketRow
                {
                    BucketLabel = r.Label,
                    Count = count,
                    Percent = total > 0 ? Math.Round(count * 100m / total, 2) : 0m
                };
            }).ToList();

            var totalPass = points.Count(x => x.IsPass);
            return new WidgetDashboardVm
            {
                SubmissionCount = total,
                OverallAvgScore = total > 0 ? Math.Round(points.Average(x => x.Score), 2) : 0m,
                OverallPassRatePercent = total > 0 ? Math.Round(totalPass * 100m / total, 2) : 0m,
                SubjectPassRates = subjectRows,
                SemesterAverages = semesterRows,
                ScoreDistribution = distRows
            };
        }

        public async Task<QuestionAnalyticsVm> GetQuestionAnalyticsAsync(DateTime fromUtc, DateTime toUtc, string? courseId = null, int minAttempts = 5)
        {
            if (minAttempts < 1) minAttempts = 1;

            var spec = new Specification<Session>(s =>
                    s.EndAt.HasValue &&
                    s.EndAt.Value >= fromUtc &&
                    s.EndAt.Value <= toUtc &&
                    s.Status != SessionStatus.NotStarted)
                .Include("Test.TestQuestions")
                .Include("StudentAnswers.Question");

            var sessions = await _sesRepo.ListAsync(spec);
            if (!string.IsNullOrWhiteSpace(courseId))
            {
                sessions = sessions.Where(s => s.Test?.CourseId == courseId).ToList();
            }

            if (!sessions.Any())
                return new QuestionAnalyticsVm();

            var sessionScoreMap = sessions.ToDictionary(s => s.Id, s => NormalizeScoreTo10(s));
            var raw = new List<(string SessionId, string QuestionId, decimal Ratio, Question? Question)>();

            foreach (var s in sessions)
            {
                if (s.StudentAnswers == null || s.StudentAnswers.Count == 0) continue;

                var testPointMap = s.Test?.TestQuestions?
                    .GroupBy(tq => tq.QuestionId)
                    .ToDictionary(g => g.Key, g => g.First().Points, StringComparer.Ordinal) ?? new Dictionary<string, decimal>(StringComparer.Ordinal);

                var fallbackPoints = s.StudentAnswers.Count > 0
                    ? (s.MaxScore > 0 ? s.MaxScore / s.StudentAnswers.Count : 1m)
                    : 1m;

                foreach (var ans in s.StudentAnswers)
                {
                    if (string.IsNullOrWhiteSpace(ans.QuestionId)) continue;

                    var points = testPointMap.TryGetValue(ans.QuestionId, out var p) ? p : fallbackPoints;
                    if (points <= 0) points = 1m;

                    var ratio = Math.Clamp(ans.Score / points, 0m, 1m);
                    raw.Add((s.Id, ans.QuestionId, ratio, ans.Question));
                }
            }

            var rows = raw
                .GroupBy(x => x.QuestionId)
                .Select(g =>
                {
                    var attempts = g.Count();
                    if (attempts < minAttempts) return null;

                    var avgRatio = g.Average(x => x.Ratio);
                    var avgPercent = Math.Round(avgRatio * 100m, 2);
                    var difficultyLabel = ResolveDifficultyLabel(avgRatio);

                    var bySession = g.GroupBy(x => x.SessionId)
                        .Select(x => new
                        {
                            SessionId = x.Key,
                            QuestionRatio = x.Average(v => v.Ratio),
                            TotalScoreNorm = sessionScoreMap.TryGetValue(x.Key, out var sc) ? sc : 0m
                        })
                        .OrderByDescending(x => x.TotalScoreNorm)
                        .ToList();

                    var discriminationIndex = CalculateDiscriminationIndex(bySession.Select(x => x.QuestionRatio).ToList());
                    var discriminationLabel = ResolveDiscriminationLabel(discriminationIndex);

                    var firstQuestion = g.Select(x => x.Question).FirstOrDefault(q => q != null);
                    var preview = firstQuestion?.Content ?? "(Missing question content)";
                    if (preview.Length > 120) preview = preview.Substring(0, 117) + "...";

                    return new QuestionAnalyticsRow
                    {
                        QuestionId = g.Key,
                        ContentPreview = preview,
                        Type = firstQuestion?.Type.ToString() ?? "(Unknown)",
                        Subject = firstQuestion?.SubjectId ?? "(Unknown)",
                        Attempts = attempts,
                        AvgScorePercent = avgPercent,
                        DifficultyLabel = difficultyLabel,
                        DiscriminationIndex = Math.Round(discriminationIndex, 3),
                        DiscriminationLabel = discriminationLabel
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .OrderBy(x => x.DiscriminationIndex)
                .ThenBy(x => x.AvgScorePercent)
                .ToList();

            return new QuestionAnalyticsVm
            {
                TotalQuestions = rows.Count,
                HardQuestions = rows.Count(x => x.DifficultyLabel == "Hard"),
                MediumQuestions = rows.Count(x => x.DifficultyLabel == "Medium"),
                EasyQuestions = rows.Count(x => x.DifficultyLabel == "Easy"),
                LowDiscriminationQuestions = rows.Count(x => x.DiscriminationIndex < 0.2m),
                Rows = rows
            };
        }

        public async Task<LecturerPerformanceVm> GetLecturerPerformanceReportAsync(DateTime fromUtc, DateTime toUtc, string? lecturerId = null)
        {
            var spec = new Specification<Session>(s =>
                    s.EndAt.HasValue &&
                    s.EndAt.Value >= fromUtc &&
                    s.EndAt.Value <= toUtc &&
                    s.Status != SessionStatus.NotStarted)
                .Include("Test.Course");

            var sessions = (await _sesRepo.ListAsync(spec))
                .Where(s => !string.IsNullOrWhiteSpace(s.Test?.Course?.LecturerId))
                .ToList();

            if (!string.IsNullOrWhiteSpace(lecturerId))
            {
                sessions = sessions.Where(s => s.Test?.Course?.LecturerId == lecturerId).ToList();
            }

            if (!sessions.Any())
                return new LecturerPerformanceVm();

            var lecturerIds = sessions
                .Select(s => s.Test?.Course?.LecturerId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var lecturers = (await _userRepo.GetAllAsync(u => lecturerIds.Contains(u.Id)))
                .ToDictionary(u => u.Id, u => u.Name ?? u.Email ?? u.Id, StringComparer.Ordinal);

            var rows = sessions
                .GroupBy(s => s.Test!.Course!.LecturerId)
                .Select(g =>
                {
                    var testIds = g.Select(x => x.TestId).Distinct(StringComparer.Ordinal).Count();
                    var courseIds = g.Select(x => x.Test!.CourseId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Count();
                    var submissionCount = g.Count();
                    var passCount = g.Count(x => x.TotalScore >= (x.Test?.PassScore ?? 5));

                    var avgScore = submissionCount > 0
                        ? Math.Round(g.Average(x => NormalizeScoreTo10(x)), 2)
                        : 0m;

                    return new LecturerPerformanceRow
                    {
                        LecturerId = g.Key,
                        LecturerName = lecturers.TryGetValue(g.Key, out var name) ? name : g.Key,
                        CourseCount = courseIds,
                        TestCount = testIds,
                        SubmissionCount = submissionCount,
                        AvgScore = avgScore,
                        PassRatePercent = submissionCount > 0 ? Math.Round(passCount * 100m / submissionCount, 2) : 0m,
                        LastSubmissionAt = g.Max(x => x.EndAt)
                    };
                })
                .OrderByDescending(x => x.SubmissionCount)
                .ThenBy(x => x.LecturerName)
                .ToList();

            return new LecturerPerformanceVm { Rows = rows };
        }

        private static decimal NormalizeScoreTo10(Session s)
        {
            if (s.MaxScore <= 0) return Math.Round(Math.Clamp(s.TotalScore, 0m, 10m), 2);
            var normalized = (s.TotalScore / s.MaxScore) * 10m;
            return Math.Round(Math.Clamp(normalized, 0m, 10m), 2);
        }

        private static string ResolveDifficultyLabel(decimal avgRatio)
        {
            if (avgRatio < 0.4m) return "Hard";
            if (avgRatio < 0.7m) return "Medium";
            return "Easy";
        }

        private static decimal CalculateDiscriminationIndex(List<decimal> orderedQuestionRatiosBySessionScore)
        {
            if (orderedQuestionRatiosBySessionScore.Count < 3)
                return 0m;

            var n = Math.Max(1, (int)Math.Round(orderedQuestionRatiosBySessionScore.Count * 0.27m, MidpointRounding.AwayFromZero));
            var upper = orderedQuestionRatiosBySessionScore.Take(n).ToList();
            var lower = orderedQuestionRatiosBySessionScore.Skip(Math.Max(0, orderedQuestionRatiosBySessionScore.Count - n)).Take(n).ToList();

            if (!upper.Any() || !lower.Any())
                return 0m;

            return upper.Average() - lower.Average();
        }

        private static string ResolveDiscriminationLabel(decimal d)
        {
            if (d >= 0.4m) return "Excellent";
            if (d >= 0.2m) return "Good";
            if (d >= 0m) return "Weak";
            return "Inverse";
        }

        private static string ResolveSubjectLabel(Session s)
        {
            if (!string.IsNullOrWhiteSpace(s.Test?.Course?.Name))
                return s.Test.Course.Name.Trim();
            if (!string.IsNullOrWhiteSpace(s.Test?.SubjectIdFilter))
                return s.Test.SubjectIdFilter.Trim();
            if (!string.IsNullOrWhiteSpace(s.Test?.Title))
                return s.Test.Title.Trim();
            return "(Unknown Subject)";
        }

        private static string ResolveSemesterLabel(Session s, IReadOnlyDictionary<string, string> enrollmentSemesterMap)
        {
            if (!string.IsNullOrWhiteSpace(s.Test?.Course?.Semester))
                return s.Test.Course.Semester.Trim();

            if (!string.IsNullOrWhiteSpace(s.Test?.CourseId))
            {
                var key = $"{s.UserId}|{s.Test.CourseId}";
                if (enrollmentSemesterMap.TryGetValue(key, out var sem) && !string.IsNullOrWhiteSpace(sem))
                    return sem.Trim();
            }

            return "(Unassigned Semester)";
        }
    }
}
