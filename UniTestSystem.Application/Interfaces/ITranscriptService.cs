using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface ITranscriptService
{
    // GPA Calculation
    Task<Transcript> CalculateGPAAsync(string studentId);
    Task<List<Transcript>> GetAllTranscriptsAsync();
    
    // Grading
    Task<bool> FinalizeCourseGradeAsync(string enrollmentId, decimal finalScore);
    
    // Retrieval
    Task<List<Enrollment>> GetStudentGradesAsync(string studentId);
    Task<Transcript?> GetStudentTranscriptSummaryAsync(string studentId);
}
