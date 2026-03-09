using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public interface IBulkImportService
{
    Task<ImportResult> ImportStudentsAsync(Stream fileStream, string? defaultClassId = null);
    Task<ImportResult> ImportCoursesAsync(Stream fileStream);
}
