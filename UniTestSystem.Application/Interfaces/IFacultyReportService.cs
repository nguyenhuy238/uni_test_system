using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface IFacultyReportService
{
    Task<FacultyReportVm> GetFacultyReportAsync(DateTime fromUtc, DateTime toUtc);
    Task<AcademicYearReportVm> GetAcademicYearReportAsync(DateTime fromUtc, DateTime toUtc);
    Task<StudentSubjectReportVm> GetStudentSubjectReportAsync(string userId, DateTime fromUtc, DateTime toUtc);
}
