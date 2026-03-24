using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using Microsoft.EntityFrameworkCore;

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

        public ReportService(
            IRepository<User> u, 
            IRepository<Student> st,
            IRepository<Test> t, 
            IRepository<Assessment> a, 
            IRepository<Session> s,
            IRepository<Question> q)
        {
            _userRepo = u; _studentRepo = st; _testRepo = t; _asRepo = a; _sesRepo = s; _qRepo = q;
        }

        // ==== Dashboard Admin ====
        public async Task<DashboardViewModel> GetAdminDashboardAsync(DateTime nowUtc)
        {
            var totalStudents = await _studentRepo.Query().CountAsync();
            var totalLecturers = await _userRepo.Query().CountAsync(x => x.Role == Role.Lecturer);
            
            var tests = await _testRepo.GetAllAsync();
            var totalTests = tests.Count;
            
            var activeAssessmentsCount = await _asRepo.Query().CountAsync(a => a.StartTime <= nowUtc && nowUtc <= a.EndTime);

            var submitted = await _sesRepo.Query()
                .Where(s => s.Status != SessionStatus.NotStarted && s.EndAt != null)
                .OrderByDescending(s => s.EndAt)
                .Take(100) // Only fetch what we might need
                .ToListAsync();

            int passed = 0;
            if (submitted.Any())
            {
                var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);
                passed = submitted.Count(s => passMap.TryGetValue(s.TestId, out var pass) && s.TotalScore >= (decimal)pass);
            }
            var passRate = submitted.Count == 0 ? 0.0m : (100.0m * passed / submitted.Count);

            var recent = submitted
                .Take(10)
                .Select(s => new DashboardViewModel.RecentSubmissionRow
                {
                    SessionId = s.Id,
                    UserName = s.UserId, // Note: In a real app, join with User table for Name
                    TestTitle = tests.FirstOrDefault(t => t.Id == s.TestId)?.Title ?? s.TestId,
                    Score = (double)s.TotalScore,
                    IsPass = tests.FirstOrDefault(t => t.Id == s.TestId)?.PassScore is int p && s.TotalScore >= (decimal)p,
                    EndAt = s.EndAt!.Value
                }).ToList();

            return new DashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalLecturers = totalLecturers,
                TotalTests = totalTests,
                ActiveAssignments = activeAssessmentsCount,
                PassRatePercent = Math.Round((double)passRate, 1),
                RecentSubmissions = recent
            };
        }

        // ==== Báo cáo theo Khoa (Faculty) ====
        public async Task<FacultyReportVm> GetFacultyReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            var passMap = await _testRepo.Query()
                .Select(t => new { t.Id, t.PassScore })
                .ToDictionaryAsync(x => x.Id, x => (decimal)x.PassScore);

            var data = await (from s in _sesRepo.Query()
                              join st in _studentRepo.Query() on s.UserId equals st.Id
                              where s.EndAt.HasValue
                                    && s.EndAt.Value >= fromUtc
                                    && s.EndAt.Value <= toUtc
                                    && s.Status != SessionStatus.NotStarted
                              select new
                              {
                                  FacultyName = st.StudentClassId ?? "(Unknown)",
                                  StudentId = s.UserId,
                                  s.TestId,
                                  s.TotalScore,
                                  s.EndAt
                              }).ToListAsync();

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
            var passMap = await _testRepo.Query()
                .Select(t => new { t.Id, t.PassScore })
                .ToDictionaryAsync(x => x.Id, x => (decimal)x.PassScore);

            var data = await (from s in _sesRepo.Query()
                              join st in _studentRepo.Query() on s.UserId equals st.Id
                              where s.EndAt.HasValue
                                    && s.EndAt.Value >= fromUtc
                                    && s.EndAt.Value <= toUtc
                                    && s.Status != SessionStatus.NotStarted
                              select new
                              {
                                  AcademicYear = st.AcademicYear ?? "(Unknown)",
                                  StudentId = s.UserId,
                                  s.TestId,
                                  s.TotalScore,
                                  s.EndAt
                              }).ToListAsync();

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
            var sessions = await _sesRepo.Query()
                .Where(s => s.UserId == userId && s.EndAt.HasValue && s.EndAt.Value >= fromUtc && s.EndAt.Value <= toUtc && s.Status != SessionStatus.NotStarted)
                .Include(s => s.StudentAnswers) 
                .ToListAsync();

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
    }
}
