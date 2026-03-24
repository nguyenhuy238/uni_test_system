using UniTestSystem.Domain;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface ITranscriptService
{
    // GPA Calculation
    Task<Transcript> CalculateGPAAsync(string studentId);
    Task<List<Transcript>> GetAllTranscriptsAsync();
    Task<List<TranscriptAdminRowVm>> GetAdminTranscriptRowsAsync(string? facultyId = null, string? classId = null, string? semester = null);
    Task<List<string>> GetAvailableSemestersAsync();
    
    // Grading
    Task<bool> FinalizeCourseGradeAsync(string enrollmentId, decimal finalScore);
    
    // Retrieval
    Task<List<Enrollment>> GetStudentGradesAsync(string studentId);
    Task<Transcript?> GetStudentTranscriptSummaryAsync(string studentId);
}
