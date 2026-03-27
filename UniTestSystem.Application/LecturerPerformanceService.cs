using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class LecturerPerformanceService : ILecturerPerformanceService
{
    private readonly IRepository<Session> _sessionRepo;
    private readonly IRepository<User> _userRepo;

    public LecturerPerformanceService(
        IRepository<Session> sessionRepo,
        IRepository<User> userRepo)
    {
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
    }

    public async Task<LecturerPerformanceVm> GetLecturerPerformanceReportAsync(DateTime fromUtc, DateTime toUtc, string? lecturerId = null)
    {
        var spec = new Specification<Session>(s =>
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted)
            .Include("Test.Course");

        var sessions = (await _sessionRepo.ListAsync(spec))
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
            .Select(group =>
            {
                var testCount = group.Select(x => x.TestId).Distinct(StringComparer.Ordinal).Count();
                var courseCount = group
                    .Select(x => x.Test!.CourseId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                var submissionCount = group.Count();
                var passCount = group.Count(x => x.TotalScore >= (x.Test?.PassScore ?? 5));

                var avgScore = submissionCount > 0
                    ? Math.Round(group.Average(x => ReportComputationHelper.NormalizeScoreTo10(x)), 2)
                    : 0m;

                return new LecturerPerformanceRow
                {
                    LecturerId = group.Key,
                    LecturerName = lecturers.TryGetValue(group.Key, out var name) ? name : group.Key,
                    CourseCount = courseCount,
                    TestCount = testCount,
                    SubmissionCount = submissionCount,
                    AvgScore = avgScore,
                    PassRatePercent = submissionCount > 0 ? Math.Round(passCount * 100m / submissionCount, 2) : 0m,
                    LastSubmissionAt = group.Max(x => x.EndAt)
                };
            })
            .OrderByDescending(x => x.SubmissionCount)
            .ThenBy(x => x.LecturerName)
            .ToList();

        return new LecturerPerformanceVm { Rows = rows };
    }
}
