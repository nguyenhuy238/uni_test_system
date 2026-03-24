using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Exam_Schedule)]
[Route("api/admin/exam-schedules")]
public class AdminExamSchedulesController : ControllerBase
{
    private readonly IExamScheduleService _scheduleService;

    public AdminExamSchedulesController(IExamScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schedules = await _scheduleService.GetAllSchedulesAsync();
        return Ok(schedules);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExamScheduleRequest request)
    {
        try
        {
            var schedule = request.ToDomain();
            await _scheduleService.CreateScheduleAsync(schedule);
            return Ok(schedule);
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
