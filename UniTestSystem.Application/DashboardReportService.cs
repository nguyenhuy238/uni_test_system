using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class DashboardReportService : IDashboardReportService
{
    private readonly IRepository<Session> _sessionRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;

    public DashboardReportService(
        IRepository<Session> sessionRepo,
        IRepository<Enrollment> enrollmentRepo)
    {
        _sessionRepo = sessionRepo;
        _enrollmentRepo = enrollmentRepo;
    }

    public async Task<WidgetDashboardVm> GetWidgetDashboardAsync(DateTime fromUtc, DateTime toUtc, Role actorRole, string? actorUserId)
    {
        var spec = new Specification<Session>(s =>
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted)
            .Include("Test.Course");

        var sessions = await _sessionRepo.ListAsync(spec);

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
            var normalizedScore = ReportComputationHelper.NormalizeScoreTo10(s);
            var passScore = s.Test?.PassScore ?? 5;
            var isPass = s.TotalScore >= passScore;
            var subject = ReportComputationHelper.ResolveSubjectLabel(s);
            var semester = ReportComputationHelper.ResolveSemesterLabel(s, enrollmentSemesterMap);
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
        var distributionRows = ranges
            .Select(range =>
            {
                var count = points.Count(x => x.Score >= range.Min && (range.IncludeMax ? x.Score <= range.Max : x.Score < range.Max));
                return new ScoreDistributionBucketRow
                {
                    BucketLabel = range.Label,
                    Count = count,
                    Percent = total > 0 ? Math.Round(count * 100m / total, 2) : 0m
                };
            })
            .ToList();

        var totalPass = points.Count(x => x.IsPass);
        return new WidgetDashboardVm
        {
            SubmissionCount = total,
            OverallAvgScore = total > 0 ? Math.Round(points.Average(x => x.Score), 2) : 0m,
            OverallPassRatePercent = total > 0 ? Math.Round(totalPass * 100m / total, 2) : 0m,
            SubjectPassRates = subjectRows,
            SemesterAverages = semesterRows,
            ScoreDistribution = distributionRows
        };
    }
}
