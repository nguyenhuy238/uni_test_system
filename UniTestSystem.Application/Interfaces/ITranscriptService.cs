using UniTestSystem.Domain;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface ITranscriptService
{
    Task<TranscriptAdminPageResult> GetAdminTranscriptPageAsync(TranscriptAdminQuery query);
    Task<StudentTranscriptDetailsResult?> GetStudentTranscriptDetailsAsync(string studentId, string? semester = null);
    Task<TranscriptExportResult> ExportAdminTranscriptOverviewXlsxAsync(TranscriptAdminQuery query);
    Task<TranscriptExportResult> ExportAdminTranscriptOverviewPdfAsync(TranscriptAdminQuery query);
    Task<TranscriptExportResult> ExportStudentTranscriptXlsxAsync(string studentId, string? semester = null);
    Task<TranscriptExportResult> ExportStudentTranscriptPdfAsync(string studentId, string? semester = null);
    Task<TranscriptExportResult> ExportMyTranscriptXlsxAsync(string studentId);
    Task<TranscriptExportResult> ExportMyTranscriptPdfAsync(string studentId);
    Task<FinalizeGradeResult> FinalizeGradeAsync(FinalizeGradeCommand command);

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

public sealed class TranscriptAdminQuery
{
    public string? FacultyId { get; set; }
    public string? ClassId { get; set; }
    public string? Semester { get; set; }
}

public sealed class TranscriptLookupResult
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class TranscriptAdminPageResult
{
    public List<TranscriptAdminRowVm> Rows { get; set; } = new();
    public List<TranscriptLookupResult> Faculties { get; set; } = new();
    public List<TranscriptLookupResult> Classes { get; set; } = new();
    public List<string> Semesters { get; set; } = new();
    public bool SchoolTranscriptLocked { get; set; }
    public Dictionary<string, bool> FacultyTranscriptLockMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool SelectedFacultyTranscriptLocked { get; set; }
}

public sealed class StudentTranscriptDetailsResult
{
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public Transcript? Summary { get; set; }
    public List<Enrollment> Grades { get; set; } = new();
    public string? Semester { get; set; }
    public List<string> Semesters { get; set; } = new();
    public bool IsTranscriptLocked { get; set; }
    public bool SchoolLocked { get; set; }
    public bool FacultyLocked { get; set; }
    public string? FacultyLockName { get; set; }
}

public sealed class TranscriptExportResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "export.bin";
}

public sealed class FinalizeGradeCommand
{
    public string EnrollmentId { get; set; } = "";
    public decimal? FinalScore { get; set; }
    public decimal? ExamScore { get; set; }
    public decimal? AssignmentScore { get; set; }
    public decimal? ExamWeight { get; set; }
    public decimal? AssignmentWeight { get; set; }
}

public sealed class FinalizeGradeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? ResolvedFinalScore { get; set; }
}
