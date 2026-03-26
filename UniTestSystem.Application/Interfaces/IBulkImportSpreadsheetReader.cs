namespace UniTestSystem.Application.Interfaces;

public interface IBulkImportSpreadsheetReader
{
    IReadOnlyList<BulkImportStudentRow> ReadStudents(Stream fileStream);
    IReadOnlyList<BulkImportCourseRow> ReadCourses(Stream fileStream);
}

public sealed class BulkImportStudentRow
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public string Major { get; set; } = "";
}

public sealed class BulkImportCourseRow
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string CreditsText { get; set; } = "";
    public string SubjectArea { get; set; } = "";
}
