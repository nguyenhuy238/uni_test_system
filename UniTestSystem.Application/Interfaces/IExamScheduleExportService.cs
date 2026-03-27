namespace UniTestSystem.Application.Interfaces;

public enum ExamScheduleExportFormat
{
    Pdf,
    Excel
}

public sealed class ExamScheduleExportFile
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "export.bin";
}

public interface IExamScheduleExportService
{
    Task<ExamScheduleExportFile?> ExportSchedulePdfAsync(string scheduleId);
    Task<ExamScheduleExportFile?> ExportScheduleExcelAsync(string scheduleId);
    Task<ExamScheduleExportFile?> ExportSchedulesZipAsync(IEnumerable<string> scheduleIds, ExamScheduleExportFormat format);
}
