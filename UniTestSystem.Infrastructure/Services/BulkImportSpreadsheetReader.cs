using ClosedXML.Excel;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Services;

public sealed class BulkImportSpreadsheetReader : IBulkImportSpreadsheetReader
{
    public IReadOnlyList<BulkImportStudentRow> ReadStudents(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1);

        return rows.Select(row => new BulkImportStudentRow
        {
            RowNumber = row.RowNumber(),
            Name = row.Cell(1).Value.ToString().Trim(),
            Email = row.Cell(2).Value.ToString().Trim(),
            StudentCode = row.Cell(3).Value.ToString().Trim(),
            Major = row.Cell(4).Value.ToString().Trim()
        }).ToList();
    }

    public IReadOnlyList<BulkImportCourseRow> ReadCourses(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1);

        return rows.Select(row => new BulkImportCourseRow
        {
            RowNumber = row.RowNumber(),
            Name = row.Cell(1).Value.ToString().Trim(),
            Code = row.Cell(2).Value.ToString().Trim(),
            CreditsText = row.Cell(3).Value.ToString().Trim(),
            SubjectArea = row.Cell(4).Value.ToString().Trim()
        }).ToList();
    }
}
