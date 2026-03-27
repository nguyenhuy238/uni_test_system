using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IReportExportService
{
    byte[] ExportFacultyYearExcel(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, DateTime from, DateTime to);
    byte[] ExportStudentSubjectExcel(StudentSubjectReportVm vm, string userName, DateTime from, DateTime to);
    byte[] ExportTranscriptOverviewExcel(IEnumerable<TranscriptAdminRowVm> rows, string? facultyName, string? className, string? semester);
    byte[] ExportStudentTranscriptExcel(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId);
    byte[] ExportFacultyYearPdf(FacultyReportVm facultyVm, AcademicYearReportVm yearVm, SystemSettings settings, DateTime from, DateTime to);
    byte[] ExportStudentSubjectPdf(StudentSubjectReportVm vm, string userName, SystemSettings settings, DateTime from, DateTime to);
    byte[] ExportTranscriptOverviewPdf(IEnumerable<TranscriptAdminRowVm> rows, SystemSettings settings, string? facultyName, string? className, string? semester);
    byte[] ExportStudentTranscriptPdf(IEnumerable<Enrollment> grades, Transcript? summary, string studentName, string studentId, SystemSettings settings);
}
