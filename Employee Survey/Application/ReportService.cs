using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Employee_Survey.Models;

namespace Employee_Survey.Application
{
    public class ReportService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Test> _testRepo;
        private readonly IRepository<Assignment> _asRepo;
        private readonly IRepository<Session> _sesRepo;

        public ReportService(IRepository<User> u, IRepository<Test> t, IRepository<Assignment> a, IRepository<Session> s)
        {
            _userRepo = u; _testRepo = t; _asRepo = a; _sesRepo = s;
        }

        // ==== Dashboard cũ (giữ nguyên, sửa nhẹ null-check) ====
        public async Task<HrDashboardViewModel> GetHrDashboardAsync(DateTime nowUtc)
        {
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();
            var assigns = await _asRepo.GetAllAsync();
            var sessions = await _sesRepo.GetAllAsync();

            var totalUsers = users.Count(x => x.Role == Role.User);
            var totalTests = tests.Count;
            var publishedTestIds = tests.Where(t => t.IsPublished).Select(t => t.Id).ToHashSet();
            var activeAssigns = assigns.Count(a => a.StartAt <= nowUtc && nowUtc <= a.EndAt && publishedTestIds.Contains(a.TestId));

            var submitted = sessions.Where(s => s.Status != SessionStatus.Draft && s.EndAt != null).ToList();
            int passed = 0;
            if (submitted.Any())
            {
                var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);
                passed = submitted.Count(s => passMap.TryGetValue(s.TestId, out var pass) && s.TotalScore >= pass);
            }
            var passRate = submitted.Count == 0 ? 0 : (100.0 * passed / submitted.Count);

            var activeList = assigns
                .Where(a => a.StartAt <= nowUtc && nowUtc <= a.EndAt && publishedTestIds.Contains(a.TestId))
                .Select(a => new HrDashboardViewModel.ActiveAssignmentRow
                {
                    TestId = a.TestId,
                    TestTitle = tests.FirstOrDefault(t => t.Id == a.TestId)?.Title ?? a.TestId,
                    Target = $"{a.TargetType}:{a.TargetValue}",
                    StartAt = a.StartAt,
                    EndAt = a.EndAt
                }).OrderBy(x => x.EndAt).Take(20).ToList();

            var recent = submitted
                .OrderByDescending(s => s.EndAt)
                .Take(10)
                .Select(s => new HrDashboardViewModel.RecentSubmissionRow
                {
                    SessionId = s.Id,
                    UserName = users.FirstOrDefault(u => u.Id == s.UserId)?.Name ?? s.UserId,
                    TestTitle = tests.FirstOrDefault(t => t.Id == s.TestId)?.Title ?? s.TestId,
                    Score = s.TotalScore,
                    IsPass = tests.FirstOrDefault(t => t.Id == s.TestId)?.PassScore is int p && s.TotalScore >= p,
                    EndAt = s.EndAt!.Value
                }).ToList();

            var skillCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in submitted)
            {
                foreach (var q in s.Snapshot ?? new List<Question>())
                {
                    var skill = string.IsNullOrWhiteSpace(q.Skill) ? "(Unknown)" : q.Skill;
                    if (!skillCount.ContainsKey(skill)) skillCount[skill] = 0;
                    skillCount[skill]++;
                }
            }
            var skills = skillCount.Select(kv => new HrDashboardViewModel.SkillStat { Skill = kv.Key, Count = kv.Value })
                                   .OrderByDescending(x => x.Count).Take(8).ToList();

            return new HrDashboardViewModel
            {
                TotalEmployees = totalUsers,
                TotalTests = totalTests,
                ActiveAssignments = activeAssigns,
                PassRatePercent = Math.Round(passRate, 1),
                ActiveAssignmentsList = activeList,
                RecentSubmissions = recent,
                TopSkills = skills
            };
        }

        // ==== NEW: Báo cáo theo Role ====
        public async Task<RoleReportVm> GetRoleReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();
            var sessions = (await _sesRepo.GetAllAsync())
                .Where(s => s.EndAt.HasValue && s.EndAt.Value >= fromUtc && s.EndAt.Value <= toUtc && s.Status != SessionStatus.Draft)
                .ToList();

            var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);

            var rows = Enum.GetValues<Role>().Select(role =>
            {
                var roleUsers = users.Where(u => u.Role == role).Select(u => u.Id).ToHashSet();
                var roleSessions = sessions.Where(s => roleUsers.Contains(s.UserId)).ToList();

                var subCount = roleSessions.Count;
                var avgScore = subCount == 0 ? 0 : roleSessions.Average(x => x.TotalScore);
                var pass = roleSessions.Count(s => passMap.TryGetValue(s.TestId, out var pass) && s.TotalScore >= pass);
                double passRate = subCount == 0 ? 0 : 100.0 * pass / subCount;
                var lastAt = roleSessions.OrderByDescending(s => s.EndAt).FirstOrDefault()?.EndAt;

                return new RoleReportRow
                {
                    Role = role.ToString(),
                    UserCount = users.Count(u => u.Role == role),
                    SubmissionCount = subCount,
                    AvgScore = Math.Round(avgScore, 2),
                    PassRatePercent = Math.Round(passRate, 1),
                    LastSubmissionAt = lastAt
                };
            }).ToList();

            return new RoleReportVm { Rows = rows };
        }

        // ==== NEW: Báo cáo theo Level ====
        public async Task<LevelReportVm> GetLevelReportAsync(DateTime fromUtc, DateTime toUtc)
        {
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();
            var sessions = (await _sesRepo.GetAllAsync())
                .Where(s => s.EndAt.HasValue && s.EndAt.Value >= fromUtc && s.EndAt.Value <= toUtc && s.Status != SessionStatus.Draft)
                .ToList();

            var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);

            var byLevel = users.GroupBy(u => string.IsNullOrWhiteSpace(u.Level) ? "(Unknown)" : u.Level);
            var rows = new List<LevelReportRow>();

            foreach (var g in byLevel)
            {
                var userIds = g.Select(u => u.Id).ToHashSet();
                var lvSessions = sessions.Where(s => userIds.Contains(s.UserId)).ToList();
                var subCount = lvSessions.Count;
                var avg = subCount == 0 ? 0 : lvSessions.Average(x => x.TotalScore);
                var pass = lvSessions.Count(s => passMap.TryGetValue(s.TestId, out var pass) && s.TotalScore >= pass);
                var last = lvSessions.OrderByDescending(s => s.EndAt).FirstOrDefault()?.EndAt;

                rows.Add(new LevelReportRow
                {
                    Level = g.Key,
                    UserCount = g.Count(),
                    SubmissionCount = subCount,
                    AvgScore = Math.Round(avg, 2),
                    PassRatePercent = Math.Round(subCount == 0 ? 0 : 100.0 * pass / subCount, 1),
                    LastSubmissionAt = last
                });
            }

            return new LevelReportVm { Rows = rows.OrderBy(r => r.Level).ToList() };
        }

        // ==== NEW: Thống kê năng lực cá nhân theo kỹ năng ====
        public async Task<UserSkillReportVm> GetUserSkillReportAsync(string userId, DateTime fromUtc, DateTime toUtc)
        {
            var sessions = (await _sesRepo.GetAllAsync())
                .Where(s => s.UserId == userId && s.EndAt.HasValue && s.EndAt.Value >= fromUtc && s.EndAt.Value <= toUtc && s.Status != SessionStatus.Draft)
                .ToList();

            var skillStats = new Dictionary<string, (int qCount, double totalScore, DateTime? last)>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in sessions)
            {
                // Map điểm từng câu từ Answers theo QId -> Score
                var scoreMap = (s.Answers ?? new List<Answer>()).ToDictionary(a => a.QuestionId, a => a.Score);
                foreach (var q in s.Snapshot ?? new List<Question>())
                {
                    var skill = string.IsNullOrWhiteSpace(q.Skill) ? "(Unknown)" : q.Skill;
                    var sc = scoreMap.TryGetValue(q.Id, out var val) ? val : 0;
                    var last = s.EndAt;

                    if (!skillStats.TryGetValue(skill, out var st)) st = (0, 0, null);
                    st.qCount += 1;
                    st.totalScore += sc;
                    st.last = (st.last == null || (last.HasValue && last.Value > st.last.Value)) ? last : st.last;
                    skillStats[skill] = st;
                }
            }

            var rows = skillStats.Select(kv => new UserSkillReportRow
            {
                Skill = kv.Key,
                QuestionCount = kv.Value.qCount,
                TotalScore = Math.Round(kv.Value.totalScore, 2),
                AvgPerQuestion = kv.Value.qCount == 0 ? 0 : Math.Round(kv.Value.totalScore / kv.Value.qCount, 2),
                LastSubmissionAt = kv.Value.last
            }).OrderByDescending(x => x.AvgPerQuestion).ToList();

            return new UserSkillReportVm { Rows = rows };
        }

        // CSV nhanh (giữ lại)
        public async Task<(string fileName, string csv)> ExportRecentSubmissionsCsvAsync()
        {
            var now = DateTime.UtcNow;
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();
            var sessions = await _sesRepo.GetAllAsync();

            var submitted = sessions.Where(s => s.Status != SessionStatus.Draft && s.EndAt != null)
                                    .OrderByDescending(s => s.EndAt)
                                    .Take(100).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SessionId,User,Test,Score,IsPass,EndAt");

            var passMap = tests.ToDictionary(t => t.Id, t => t.PassScore);

            foreach (var s in submitted)
            {
                var uname = users.FirstOrDefault(u => u.Id == s.UserId)?.Name ?? s.UserId;
                var ttitle = tests.FirstOrDefault(t => t.Id == s.TestId)?.Title ?? s.TestId;
                var isPass = passMap.TryGetValue(s.TestId, out var pass) && s.TotalScore >= pass;
                sb.AppendLine($"{s.Id},\"{uname.Replace("\"", "\"\"")}\",\"{ttitle.Replace("\"", "\"\"")}\",{s.TotalScore},{isPass},{s.EndAt:O}");
            }
            return ($"recent-submissions-{now:yyyyMMddHHmmss}.csv", sb.ToString());
        }
    }
}
