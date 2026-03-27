using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IReportsUseCaseService
{
    Task<ReportsIndexVm> GetIndexVmAsync(DateTime fromUtc, DateTime toUtc, Role actorRole, string? actorUserId);
    Task<QuestionAnalyticsVm> GetQuestionAnalyticsVmAsync(DateTime fromUtc, DateTime toUtc, string? courseId, int minAttempts);
    Task<LecturerPerformanceVm> GetLecturerPerformanceVmAsync(DateTime fromUtc, DateTime toUtc, string? lecturerId);
    Task<StudentSubjectReportVm> GetStudentSubjectVmAsync(string userId, DateTime fromUtc, DateTime toUtc);

    Task<List<Course>> GetActiveCoursesAsync();
    Task<List<User>> GetActiveLecturersAsync();
    Task<User?> GetUserByIdAsync(string userId);

    Task<byte[]> ExportFacultyYearExcelAsync(DateTime fromUtc, DateTime toUtc);
    Task<byte[]> ExportFacultyYearPdfAsync(DateTime fromUtc, DateTime toUtc);
    Task<byte[]> ExportStudentSubjectExcelAsync(string userId, DateTime fromUtc, DateTime toUtc);
    Task<byte[]> ExportStudentSubjectPdfAsync(string userId, DateTime fromUtc, DateTime toUtc);
}
