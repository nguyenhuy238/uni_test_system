using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class FacultyReportService : IFacultyReportService
{
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<StudentClass> _classRepo;
    private readonly IRepository<Faculty> _facultyRepo;
    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<Session> _sessionRepo;

    public FacultyReportService(
        IRepository<Student> studentRepo,
        IRepository<StudentClass> classRepo,
        IRepository<Faculty> facultyRepo,
        IRepository<Test> testRepo,
        IRepository<Session> sessionRepo)
    {
        _studentRepo = studentRepo;
        _classRepo = classRepo;
        _facultyRepo = facultyRepo;
        _testRepo = testRepo;
        _sessionRepo = sessionRepo;
    }

    public async Task<FacultyReportVm> GetFacultyReportAsync(DateTime fromUtc, DateTime toUtc)
    {
        var passMap = (await _testRepo.GetAllAsync())
            .ToDictionary(x => x.Id, x => (decimal)x.PassScore, StringComparer.Ordinal);

        var sessions = await _sessionRepo.GetAllAsync(s =>
            s.EndAt.HasValue &&
            s.EndAt.Value >= fromUtc &&
            s.EndAt.Value <= toUtc &&
            s.Status != SessionStatus.NotStarted);

        var students = await _studentRepo.GetAllAsync(s => !s.IsDeleted);
        var classes = await _classRepo.GetAllAsync(c => !c.IsDeleted);
        var faculties = await _facultyRepo.GetAllAsync(f => !f.IsDeleted);

        var classById = classes.ToDictionary(c => c.Id, c => c, StringComparer.Ordinal);
        var facultyById = faculties.ToDictionary(f => f.Id, f => f.Name, StringComparer.Ordinal);

        var facultyNameByStudent = students.ToDictionary(
            student => student.Id,
            student =>
            {
                if (string.IsNullOrWhiteSpace(student.StudentClassId))
                    return "(Unknown Faculty)";

                if (!classById.TryGetValue(student.StudentClassId, out var studentClass))
                    return "(Unknown Faculty)";

                if (!string.IsNullOrWhiteSpace(studentClass.FacultyId) &&
                    facultyById.TryGetValue(studentClass.FacultyId, out var facultyName))
                {
                    return facultyName;
                }

                return "(Unknown Faculty)";
            },
            StringComparer.Ordinal);

        var data = sessions
            .Where(s => facultyNameByStudent.ContainsKey(s.UserId))
            .Select(s => new
            {
                FacultyName = facultyNameByStudent[s.UserId],
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
                var submissionCount = g.Count();
                return new FacultyReportRow
                {
                    FacultyName = g.Key,
                    StudentCount = g.Select(x => x.StudentId).Distinct(StringComparer.Ordinal).Count(),
                    SubmissionCount = submissionCount,
                    AvgScore = submissionCount > 0 ? Math.Round(g.Average(x => x.TotalScore), 2) : 0m,
                    PassRatePercent = submissionCount > 0 ? Math.Round((passCount * 100m) / submissionCount, 2) : 0m,
                    LastSubmissionAt = g.Max(x => x.EndAt)
                };
            })
            .OrderBy(x => x.FacultyName)
            .ToList();

        return new FacultyReportVm { Rows = rows };
    }

    public async Task<AcademicYearReportVm> GetAcademicYearReportAsync(DateTime fromUtc, DateTime toUtc)
    {
        var passMap = (await _testRepo.GetAllAsync())
            .ToDictionary(x => x.Id, x => (decimal)x.PassScore, StringComparer.Ordinal);

        var sessions = await _sessionRepo.GetAllAsync(s =>
            s.EndAt.HasValue &&
            s.EndAt.Value >= fromUtc &&
            s.EndAt.Value <= toUtc &&
            s.Status != SessionStatus.NotStarted);

        var students = await _studentRepo.GetAllAsync(s => !s.IsDeleted);
        var studentYearMap = students.ToDictionary(
            st => st.Id,
            st => string.IsNullOrWhiteSpace(st.AcademicYear) ? "(Unknown)" : st.AcademicYear!,
            StringComparer.Ordinal);

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
                var submissionCount = g.Count();
                return new AcademicYearReportRow
                {
                    AcademicYear = g.Key,
                    StudentCount = g.Select(x => x.StudentId).Distinct(StringComparer.Ordinal).Count(),
                    SubmissionCount = submissionCount,
                    AvgScore = submissionCount > 0 ? Math.Round(g.Average(x => x.TotalScore), 2) : 0m,
                    PassRatePercent = submissionCount > 0 ? Math.Round((passCount * 100m) / submissionCount, 2) : 0m,
                    LastSubmissionAt = g.Max(x => x.EndAt)
                };
            })
            .OrderBy(x => x.AcademicYear)
            .ToList();

        return new AcademicYearReportVm { Rows = rows };
    }

    public async Task<StudentSubjectReportVm> GetStudentSubjectReportAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        var spec = new Specification<Session>(s =>
                s.UserId == userId &&
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted)
            .Include(s => s.StudentAnswers!);

        var sessions = await _sessionRepo.ListAsync(spec);

        var subjectData = new Dictionary<string, (int QuestionCount, decimal TotalScore, DateTime? LastSubmissionAt)>(StringComparer.OrdinalIgnoreCase);

        foreach (var session in sessions)
        {
            foreach (var answer in session.StudentAnswers)
            {
                var subject = "General Academic";
                if (!subjectData.TryGetValue(subject, out var data))
                    data = (0, 0m, null);

                data.QuestionCount++;
                data.TotalScore += answer.Score;
                if (data.LastSubmissionAt == null || session.EndAt > data.LastSubmissionAt)
                    data.LastSubmissionAt = session.EndAt;

                subjectData[subject] = data;
            }
        }

        var rows = subjectData
            .Select(kvp => new StudentSubjectReportRow
            {
                Subject = kvp.Key,
                QuestionCount = kvp.Value.QuestionCount,
                TotalScore = kvp.Value.TotalScore,
                AvgPerQuestion = kvp.Value.QuestionCount > 0
                    ? Math.Round(kvp.Value.TotalScore / kvp.Value.QuestionCount, 2)
                    : 0m,
                LastSubmissionAt = kvp.Value.LastSubmissionAt
            })
            .OrderBy(r => r.Subject)
            .ToList();

        return new StudentSubjectReportVm { Rows = rows };
    }
}
