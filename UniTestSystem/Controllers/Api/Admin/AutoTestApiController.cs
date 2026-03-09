using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = UniTestSystem.Domain.User;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,HR,Manager")]
[Route("api/admin/autotests")]
public class AutoTestApiController : ControllerBase
{
    private readonly ITestGenerationService _svc;
    private readonly IRepository<Student> _sRepo;
    private readonly IRepository<Test> _tRepo;
    private readonly IRepository<Assessment> _asRepo;

    public AutoTestApiController(
        ITestGenerationService svc,
        IRepository<Student> sRepo,
        IRepository<Test> tRepo,
        IRepository<Assessment> asRepo)
    {
        _svc = svc;
        _sRepo = sRepo;
        _tRepo = tRepo;
        _asRepo = asRepo;
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var students = await _sRepo.GetAllAsync();
        var depts = students
            .Select(u => u.StudentClassId ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
        return Ok(depts);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] AutoTestOptions opt)
    {
        var actor = User.Identity?.Name ?? "admin";
        try
        {
            var results = await _svc.GeneratePersonalizedAsync(opt, actor);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("assign-batch")]
    public async Task<IActionResult> AssignBatch([FromBody] AssignBatchRequest req)
    {
        if (req.TestIds.Count != req.UserIds.Count) return BadRequest("Lists must have same length");

        var s = req.StartAtUtc ?? DateTime.UtcNow.AddDays(-1);
        var e = req.EndAtUtc ?? DateTime.UtcNow.AddDays(30);

        for (int i = 0; i < req.TestIds.Count; i++)
        {
            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == req.TestIds[i]);
            if (t == null) continue;

            if (!t.IsPublished)
            {
                t.IsPublished = true;
                t.PublishedAt = DateTime.UtcNow;
                await _tRepo.UpsertAsync(x => x.Id == t.Id, t);
            }

            var assessment = new Assessment
            {
                Title = t.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = req.UserIds[i],
                CourseId = t.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _asRepo.InsertAsync(assessment);
            
            t.AssessmentId = assessment.Id;
            await _tRepo.UpsertAsync(x => x.Id == t.Id, t);
        }

        return Ok(new { message = $"Successfully assigned {req.TestIds.Count} tests." });
    }
}

public class AssignBatchRequest
{
    public List<string> TestIds { get; set; } = new();
    public List<string> UserIds { get; set; } = new();
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
}
