using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Exam_Schedule)]
[Route("api/admin/exam-schedules")]
public class AdminExamSchedulesController : ControllerBase
{
    private readonly IExamScheduleService _scheduleService;
    private readonly IExamScheduleExportService _exportService;

    public AdminExamSchedulesController(IExamScheduleService scheduleService, IExamScheduleExportService exportService)
    {
        _scheduleService = scheduleService;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schedules = await _scheduleService.GetAllSchedulesAsync();
        return Ok(schedules.Select(MapScheduleDto).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExamScheduleRequest request)
    {
        try
        {
            var schedule = request.ToDomain();
            await _scheduleService.CreateScheduleAsync(schedule);

            var created = await _scheduleService.GetScheduleByIdAsync(schedule.Id);
            return Ok(MapScheduleDto(created ?? schedule));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateExamScheduleRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "Bulk payload is empty." });
        }

        var results = new List<BulkCreateExamScheduleResultItem>();
        foreach (var item in request.Items)
        {
            try
            {
                var schedule = item.ToDomain();
                await _scheduleService.CreateScheduleAsync(schedule);
                results.Add(new BulkCreateExamScheduleResultItem
                {
                    CourseId = item.CourseId,
                    TestId = item.TestId,
                    Room = item.Room,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkCreateExamScheduleResultItem
                {
                    CourseId = item.CourseId,
                    TestId = item.TestId,
                    Room = item.Room,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            total = results.Count,
            success = results.Count(r => r.Success),
            failed = results.Count(r => !r.Success),
            items = results
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _scheduleService.DeleteScheduleAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> Lock(string id)
    {
        var staffId = ResolveActor();
        var ok = await _scheduleService.LockScheduleAsync(id, staffId);
        if (!ok)
        {
            return NotFound();
        }

        return Ok(new { message = "Schedule locked." });
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(string id)
    {
        var staffId = ResolveActor();
        var ok = await _scheduleService.UnlockScheduleAsync(id, staffId);
        if (!ok)
        {
            return NotFound();
        }

        return Ok(new { message = "Schedule unlocked." });
    }

    [HttpGet("{id}/export/pdf")]
    public async Task<IActionResult> ExportPdfById(string id)
    {
        var file = await _exportService.ExportSchedulePdfAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("{id}/export/excel")]
    public async Task<IActionResult> ExportExcelById(string id)
    {
        var file = await _exportService.ExportScheduleExcelAsync(id);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("export/zip")]
    public async Task<IActionResult> ExportZip([FromBody] BulkExportExamScheduleRequest request)
    {
        if (request.ScheduleIds == null || request.ScheduleIds.Count == 0)
        {
            return BadRequest(new { message = "scheduleIds is required." });
        }

        if (!TryParseFormat(request.Format, out var format))
        {
            return BadRequest(new { message = "format must be 'pdf' or 'excel'." });
        }

        var file = await _exportService.ExportSchedulesZipAsync(request.ScheduleIds, format);
        if (file == null)
        {
            return NotFound(new { message = "No schedules were exported." });
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var schedules = await _scheduleService.GetAllSchedulesAsync();
        var csv = new StringBuilder();
        csv.AppendLine("CourseCode,CourseName,TestTitle,Room,StartTimeUtc,EndTimeUtc,ExamType");

        foreach (var schedule in schedules.OrderBy(s => s.StartTime))
        {
            csv.AppendLine(
                $"{EscapeCsv(schedule.Course?.Code)}," +
                $"{EscapeCsv(schedule.Course?.Name)}," +
                $"{EscapeCsv(schedule.Test?.Title)}," +
                $"{EscapeCsv(schedule.Room)}," +
                $"{schedule.StartTime:O}," +
                $"{schedule.EndTime:O}," +
                $"{EscapeCsv(schedule.ExamType)}");
        }

        var fileName = $"exam-schedules-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
    }

    private static string EscapeCsv(string? value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }

    private string ResolveActor()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown";
    }

    private static bool TryParseFormat(string? raw, out ExamScheduleExportFormat format)
    {
        if (string.Equals(raw, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            format = ExamScheduleExportFormat.Pdf;
            return true;
        }

        if (string.Equals(raw, "excel", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            format = ExamScheduleExportFormat.Excel;
            return true;
        }

        format = default;
        return false;
    }

    private static AdminExamScheduleDto MapScheduleDto(ExamSchedule schedule)
    {
        return new AdminExamScheduleDto
        {
            Id = schedule.Id,
            TestId = schedule.TestId,
            CourseId = schedule.CourseId,
            Room = schedule.Room,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            ExamType = schedule.ExamType,
            IsManuallyLocked = schedule.IsManuallyLocked,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt,
            Course = schedule.Course == null
                ? null
                : new AdminExamScheduleCourseDto
                {
                    Id = schedule.Course.Id,
                    Code = schedule.Course.Code,
                    Name = schedule.Course.Name,
                    SubjectArea = schedule.Course.SubjectArea,
                    Semester = schedule.Course.Semester
                },
            Test = schedule.Test == null
                ? null
                : new AdminExamScheduleTestDto
                {
                    Id = schedule.Test.Id,
                    Title = schedule.Test.Title,
                    Type = schedule.Test.Type.ToString(),
                    AssessmentType = schedule.Test.AssessmentType.ToString(),
                    DurationMinutes = schedule.Test.DurationMinutes,
                    PassScore = schedule.Test.PassScore,
                    IsPublished = schedule.Test.IsPublished
                }
        };
    }
}

public class AdminExamScheduleDto
{
    public string Id { get; set; } = "";
    public string TestId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string Room { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ExamType { get; set; } = "Final";
    public bool IsManuallyLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AdminExamScheduleCourseDto? Course { get; set; }
    public AdminExamScheduleTestDto? Test { get; set; }
}

public class AdminExamScheduleCourseDto
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string SubjectArea { get; set; } = "";
    public string Semester { get; set; } = "";
}

public class AdminExamScheduleTestDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string AssessmentType { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int PassScore { get; set; }
    public bool IsPublished { get; set; }
}

public class CreateExamScheduleRequest
{
    public string TestId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string Room { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ExamType { get; set; } = "Final";

    public ExamSchedule ToDomain()
    {
        return new ExamSchedule
        {
            TestId = TestId,
            CourseId = CourseId,
            Room = Room,
            StartTime = StartTime,
            EndTime = EndTime,
            ExamType = string.IsNullOrWhiteSpace(ExamType) ? "Final" : ExamType.Trim()
        };
    }
}

public class BulkCreateExamScheduleRequest
{
    public List<CreateExamScheduleRequest> Items { get; set; } = new();
}

public class BulkCreateExamScheduleResultItem
{
    public string CourseId { get; set; } = "";
    public string TestId { get; set; } = "";
    public string Room { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class BulkExportExamScheduleRequest
{
    public List<string> ScheduleIds { get; set; } = new();
    public string Format { get; set; } = "pdf";
}
