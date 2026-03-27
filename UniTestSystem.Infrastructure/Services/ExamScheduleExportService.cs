using System.IO.Compression;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Infrastructure.Services;

public class ExamScheduleExportService : IExamScheduleExportService
{
    private readonly IRepository<ExamSchedule> _scheduleRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<StudentClass> _classRepo;
    private readonly ISettingsService _settingsService;

    public ExamScheduleExportService(
        IRepository<ExamSchedule> scheduleRepo,
        IRepository<Enrollment> enrollmentRepo,
        IRepository<Student> studentRepo,
        IRepository<User> userRepo,
        IRepository<StudentClass> classRepo,
        ISettingsService settingsService)
    {
        _scheduleRepo = scheduleRepo;
        _enrollmentRepo = enrollmentRepo;
        _studentRepo = studentRepo;
        _userRepo = userRepo;
        _classRepo = classRepo;
        _settingsService = settingsService;
    }

    public async Task<ExamScheduleExportFile?> ExportSchedulePdfAsync(string scheduleId)
    {
        var data = await BuildExportDataAsync(scheduleId);
        if (data == null)
        {
            return null;
        }

        var content = BuildPdf(data);
        var prefix = BuildFilePrefix(data.Schedule);
        return new ExamScheduleExportFile
        {
            Content = content,
            ContentType = "application/pdf",
            FileName = $"{prefix}.pdf"
        };
    }

    public async Task<ExamScheduleExportFile?> ExportScheduleExcelAsync(string scheduleId)
    {
        var data = await BuildExportDataAsync(scheduleId);
        if (data == null)
        {
            return null;
        }

        var content = BuildExcel(data);
        var prefix = BuildFilePrefix(data.Schedule);
        return new ExamScheduleExportFile
        {
            Content = content,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"{prefix}.xlsx"
        };
    }

    public async Task<ExamScheduleExportFile?> ExportSchedulesZipAsync(IEnumerable<string> scheduleIds, ExamScheduleExportFormat format)
    {
        var ids = scheduleIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return null;
        }

        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var scheduleId in ids)
            {
                var file = format == ExamScheduleExportFormat.Pdf
                    ? await ExportSchedulePdfAsync(scheduleId)
                    : await ExportScheduleExcelAsync(scheduleId);

                if (file == null || file.Content.Length == 0)
                {
                    continue;
                }

                var entryName = EnsureUniqueEntryName(file.FileName, usedNames);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(file.Content, 0, file.Content.Length);
            }

            if (archive.Entries.Count == 0)
            {
                return null;
            }
        }

        return new ExamScheduleExportFile
        {
            Content = memory.ToArray(),
            ContentType = "application/zip",
            FileName = $"exam-schedules-{format.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip"
        };
    }

    private async Task<ExamScheduleExportData?> BuildExportDataAsync(string scheduleId)
    {
        var spec = new Specification<ExamSchedule>(s => s.Id == scheduleId && !s.IsDeleted)
            .Include(s => s.Course!)
            .Include(s => s.Test!);

        var schedule = await _scheduleRepo.FirstOrDefaultAsync(spec);
        if (schedule == null)
        {
            return null;
        }

        var settings = await _settingsService.GetAsync();

        var enrollments = await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted && e.CourseId == schedule.CourseId);
        var studentIds = enrollments
            .Select(e => e.StudentId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var students = await _studentRepo.GetAllAsync(s => !s.IsDeleted && studentIds.Contains(s.Id));
        var users = await _userRepo.GetAllAsync(u => !u.IsDeleted && studentIds.Contains(u.Id));
        var classIds = students
            .Select(s => s.StudentClassId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var classes = await _classRepo.GetAllAsync(c => !c.IsDeleted && classIds.Contains(c.Id));

        var studentMap = students.ToDictionary(s => s.Id, s => s, StringComparer.Ordinal);
        var userMap = users.ToDictionary(u => u.Id, u => u, StringComparer.Ordinal);
        var classMap = classes.ToDictionary(c => c.Id, c => c, StringComparer.Ordinal);

        var rows = studentIds
            .Select(studentId => BuildRow(schedule, studentId, studentMap, userMap, classMap))
            .Where(x => x != null)
            .Select((row, index) =>
            {
                row!.Order = index + 1;
                return row;
            })
            .OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.StudentCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
            .Select((row, index) =>
            {
                row.Order = index + 1;
                return row;
            })
            .ToList();

        return new ExamScheduleExportData
        {
            SchoolName = settings.SystemName,
            Schedule = schedule,
            Rows = rows
        };
    }

    private static ExamScheduleExportRow? BuildRow(
        ExamSchedule schedule,
        string studentId,
        IReadOnlyDictionary<string, Student> studentMap,
        IReadOnlyDictionary<string, User> userMap,
        IReadOnlyDictionary<string, StudentClass> classMap)
    {
        var hasStudent = studentMap.TryGetValue(studentId, out var student);
        userMap.TryGetValue(studentId, out var user);

        if (!hasStudent && user == null)
        {
            return null;
        }

        StudentClass? studentClass = null;
        if (student != null && !string.IsNullOrWhiteSpace(student.StudentClassId))
        {
            classMap.TryGetValue(student.StudentClassId, out studentClass);
        }

        var studentCode = !string.IsNullOrWhiteSpace(student?.StudentCode) ? student.StudentCode : studentId;
        var fullName = user?.Name ?? student?.Name ?? studentId;
        var className = studentClass?.Code ?? studentClass?.Name ?? "-";
        var examTime = $"{schedule.StartTime:yyyy-MM-dd HH:mm} - {schedule.EndTime:HH:mm}";
        var examPaper = schedule.Test?.Title ?? schedule.TestId;

        return new ExamScheduleExportRow
        {
            StudentId = studentId,
            StudentCode = studentCode,
            FullName = fullName,
            ClassName = className,
            Room = schedule.Room,
            ExamTime = examTime,
            ExamPaper = examPaper
        };
    }

    private static byte[] BuildExcel(ExamScheduleExportData data)
    {
        using var workbook = new XLWorkbook();
        var detail = workbook.AddWorksheet("Danh sach thi sinh");

        detail.Cell(1, 1).Value = (data.SchoolName ?? "TRUONG DAI HOC").ToUpperInvariant();
        detail.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.FontSize = 14;

        detail.Cell(2, 1).Value = $"Ky thi: {data.Schedule.ExamType} - {data.Schedule.Test?.Title ?? data.Schedule.TestId}";
        detail.Range(2, 1, 2, 7).Merge().Style.Font.SetBold();

        detail.Cell(3, 1).Value = $"Mon: {data.Schedule.Course?.Name ?? data.Schedule.CourseId}";
        detail.Cell(3, 4).Value = $"Phong thi: {data.Schedule.Room}";
        detail.Cell(3, 6).Value = $"Gio thi: {data.Schedule.StartTime:yyyy-MM-dd HH:mm}";
        detail.Range(3, 6, 3, 7).Merge();

        WriteTableHeader(detail, 5);

        var rowIndex = 6;
        foreach (var row in data.Rows)
        {
            detail.Cell(rowIndex, 1).Value = row.Order;
            detail.Cell(rowIndex, 2).Value = row.StudentCode;
            detail.Cell(rowIndex, 3).Value = row.FullName;
            detail.Cell(rowIndex, 4).Value = row.ClassName;
            detail.Cell(rowIndex, 5).Value = row.Room;
            detail.Cell(rowIndex, 6).Value = row.ExamTime;
            detail.Cell(rowIndex, 7).Value = row.ExamPaper;
            rowIndex++;
        }

        detail.SheetView.FreezeRows(5);
        detail.Columns().AdjustToContents();

        var summary = workbook.AddWorksheet("Tong hop theo phong");
        summary.Cell(1, 1).Value = "Tong hop thi sinh theo phong thi";
        summary.Range(1, 1, 1, 3).Merge().Style.Font.SetBold().Font.FontSize = 13;
        summary.Cell(3, 1).Value = "Phong";
        summary.Cell(3, 2).Value = "So thi sinh";
        summary.Cell(3, 3).Value = "Khung gio";
        summary.Range(3, 1, 3, 3).Style.Font.SetBold();
        summary.Range(3, 1, 3, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");

        summary.Cell(4, 1).Value = data.Schedule.Room;
        summary.Cell(4, 2).Value = data.Rows.Count;
        summary.Cell(4, 3).Value = $"{data.Schedule.StartTime:yyyy-MM-dd HH:mm} - {data.Schedule.EndTime:HH:mm}";
        summary.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteTableHeader(IXLWorksheet sheet, int rowIndex)
    {
        sheet.Cell(rowIndex, 1).Value = "STT";
        sheet.Cell(rowIndex, 2).Value = "Ma SV";
        sheet.Cell(rowIndex, 3).Value = "Ho ten";
        sheet.Cell(rowIndex, 4).Value = "Lop";
        sheet.Cell(rowIndex, 5).Value = "Phong thi";
        sheet.Cell(rowIndex, 6).Value = "Gio thi";
        sheet.Cell(rowIndex, 7).Value = "De thi";

        var headerRange = sheet.Range(rowIndex, 1, rowIndex, 7);
        headerRange.Style.Font.SetBold();
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static byte[] BuildPdf(ExamScheduleExportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);

                page.Header().Row(row =>
                {
                    row.ConstantItem(70).Height(45).Border(1).AlignCenter().AlignMiddle().Text("LOGO").SemiBold();
                    row.RelativeItem().PaddingLeft(10).Column(col =>
                    {
                        col.Item().Text((data.SchoolName ?? "TRUONG DAI HOC").ToUpperInvariant()).FontSize(16).SemiBold();
                        col.Item().Text($"Ky thi: {data.Schedule.ExamType} - {data.Schedule.Test?.Title ?? data.Schedule.TestId}");
                        col.Item().Text($"Mon: {data.Schedule.Course?.Name ?? data.Schedule.CourseId} | Phong: {data.Schedule.Room} | Gio: {data.Schedule.StartTime:yyyy-MM-dd HH:mm}");
                    });
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.7f);
                        columns.RelativeColumn(2.2f);
                    });

                    table.Header(header =>
                    {
                        WriteHeaderCell(header, "STT");
                        WriteHeaderCell(header, "Ma SV");
                        WriteHeaderCell(header, "Ho ten");
                        WriteHeaderCell(header, "Lop");
                        WriteHeaderCell(header, "Phong thi");
                        WriteHeaderCell(header, "Gio thi");
                        WriteHeaderCell(header, "De thi");
                    });

                    foreach (var row in data.Rows)
                    {
                        WriteBodyCell(table, row.Order.ToString());
                        WriteBodyCell(table, row.StudentCode);
                        WriteBodyCell(table, row.FullName);
                        WriteBodyCell(table, row.ClassName);
                        WriteBodyCell(table, row.Room);
                        WriteBodyCell(table, row.ExamTime);
                        WriteBodyCell(table, row.ExamPaper);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void WriteHeaderCell(TableCellDescriptor header, string value)
    {
        header.Cell().Background(Colors.Grey.Lighten3).Border(1).Padding(4).Text(value).SemiBold().FontSize(9);
    }

    private static void WriteBodyCell(TableDescriptor table, string value)
    {
        table.Cell().Border(1).Padding(4).Text(value).FontSize(9);
    }

    private static string BuildFilePrefix(ExamSchedule schedule)
    {
        var course = SanitizeFileNamePart(schedule.Course?.Code ?? schedule.CourseId);
        var test = SanitizeFileNamePart(schedule.Test?.Title ?? schedule.TestId);
        return $"exam-schedule-{course}-{test}-{schedule.StartTime:yyyyMMddHHmm}";
    }

    private static string EnsureUniqueEntryName(string fileName, ISet<string> usedNames)
    {
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "schedule-export.bin" : fileName.Trim();
        if (usedNames.Add(safeName))
        {
            return safeName;
        }

        var extension = Path.GetExtension(safeName);
        var baseName = Path.GetFileNameWithoutExtension(safeName);
        var suffix = 2;
        while (true)
        {
            var candidate = $"{baseName}-{suffix}{extension}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static string SanitizeFileNamePart(string raw)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((raw ?? string.Empty).Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
    }

    private sealed class ExamScheduleExportData
    {
        public string? SchoolName { get; set; }
        public ExamSchedule Schedule { get; set; } = new();
        public List<ExamScheduleExportRow> Rows { get; set; } = new();
    }

    private sealed class ExamScheduleExportRow
    {
        public int Order { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string ExamTime { get; set; } = string.Empty;
        public string ExamPaper { get; set; } = string.Empty;
    }
}
