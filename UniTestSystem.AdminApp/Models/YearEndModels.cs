using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models;

public sealed class YearEndPreviewModel
{
    [JsonPropertyName("academicYear")]
    public string AcademicYear { get; set; } = "";

    [JsonPropertyName("facultyId")]
    public string? FacultyId { get; set; }

    [JsonPropertyName("facultyName")]
    public string? FacultyName { get; set; }

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }

    [JsonPropertyName("prerequisites")]
    public YearEndPrerequisiteModel Prerequisites { get; set; } = new();

    [JsonPropertyName("students")]
    public List<YearEndStudentSummaryModel> Students { get; set; } = new();

    [JsonPropertyName("warningStudents")]
    public List<YearEndStudentSummaryModel> WarningStudents { get; set; } = new();

    [JsonPropertyName("totalStudents")]
    public int TotalStudents { get; set; }
}

public sealed class YearEndFinalizeResultModel
{
    [JsonPropertyName("academicYear")]
    public string AcademicYear { get; set; } = "";

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("finalizedStudents")]
    public int FinalizedStudents { get; set; }

    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = new();
}

public sealed class YearEndPrerequisiteModel
{
    [JsonPropertyName("isReady")]
    public bool IsReady { get; set; }

    [JsonPropertyName("allExamSchedulesCompleted")]
    public bool AllExamSchedulesCompleted { get; set; }

    [JsonPropertyName("allEssaysGraded")]
    public bool AllEssaysGraded { get; set; }

    [JsonPropertyName("semesterTranscriptsLocked")]
    public bool SemesterTranscriptsLocked { get; set; }

    [JsonPropertyName("incompleteExamScheduleCount")]
    public int IncompleteExamScheduleCount { get; set; }

    [JsonPropertyName("pendingEssayCount")]
    public int PendingEssayCount { get; set; }

    [JsonPropertyName("unlockedSemesterTranscriptCount")]
    public int UnlockedSemesterTranscriptCount { get; set; }

    [JsonPropertyName("missingItems")]
    public List<string> MissingItems { get; set; } = new();
}

public sealed class YearEndStudentSummaryModel
{
    [JsonPropertyName("studentId")]
    public string StudentId { get; set; } = "";

    [JsonPropertyName("studentName")]
    public string StudentName { get; set; } = "";

    [JsonPropertyName("facultyName")]
    public string FacultyName { get; set; } = "";

    [JsonPropertyName("className")]
    public string ClassName { get; set; } = "";

    [JsonPropertyName("yearEndGpa4")]
    public decimal YearEndGpa4 { get; set; }

    [JsonPropertyName("yearEndGpa10")]
    public decimal YearEndGpa10 { get; set; }

    [JsonPropertyName("totalCreditsEarned")]
    public int TotalCreditsEarned { get; set; }

    [JsonPropertyName("academicStatus")]
    public string AcademicStatus { get; set; } = "";
}
