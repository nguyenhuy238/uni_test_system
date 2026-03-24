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
    decimal CalculateWeightedFinalScore(decimal assignmentScore, decimal examScore, decimal assignmentWeightPercent, decimal examWeightPercent);

    // Transcript Locking
    Task<bool> IsSchoolTranscriptLockedAsync();
    Task<bool> IsFacultyTranscriptLockedAsync(string facultyId);
    Task<Dictionary<string, bool>> GetFacultyTranscriptLockMapAsync();
    Task LockSchoolTranscriptAsync(string actor, string? note = null);
    Task UnlockSchoolTranscriptAsync(string actor, string? note = null);
    Task LockFacultyTranscriptAsync(string facultyId, string actor, string? note = null);
    Task UnlockFacultyTranscriptAsync(string facultyId, string actor, string? note = null);
    
    // Retrieval
    Task<List<Enrollment>> GetStudentGradesAsync(string studentId);
    Task<Transcript?> GetStudentTranscriptSummaryAsync(string studentId);
}
