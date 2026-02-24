using Employee_Survey.Application;
using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = Employee_Survey.Domain.User;

namespace Employee_Survey.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,HR,Manager")]
[Route("api/admin/autotests")]
public class AutoTestApiController : ControllerBase
{
    private readonly ITestGenerationService _svc;
    private readonly IRepository<DomainUser> _uRepo;
    private readonly IRepository<Test> _tRepo;
    private readonly IRepository<Assignment> _aRepo;

    public AutoTestApiController(
        ITestGenerationService svc,
        IRepository<DomainUser> uRepo,
        IRepository<Test> tRepo,
        IRepository<Assignment> aRepo)
    {
        _svc = svc;
        _uRepo = uRepo;
        _tRepo = tRepo;
        _aRepo = aRepo;
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var users = await _uRepo.GetAllAsync();
        var depts = users
            .Select(u => u.Department ?? "")
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

            await _aRepo.InsertAsync(new Assignment
            {
                TestId = req.TestIds[i],
                TargetType = "User",
                TargetValue = req.UserIds[i],
                StartAt = s,
                EndAt = e
            });
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
