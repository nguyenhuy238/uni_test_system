using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,HR,Manager")]
[Route("api/admin/autotests")]
public class AutoTestApiController : ControllerBase
{
    private readonly ITestGenerationService _svc;
    private readonly ITestAdministrationService _testAdministrationService;

    public AutoTestApiController(
        ITestGenerationService svc,
        ITestAdministrationService testAdministrationService)
    {
        _svc = svc;
        _testAdministrationService = testAdministrationService;
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        return Ok(await _testAdministrationService.GetDepartmentOptionsAsync());
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

        var assignments = req.TestIds
            .Select((testId, index) => new TestUserAssignment
            {
                TestId = testId,
                UserId = req.UserIds[index]
            })
            .ToList();

        var assigned = await _testAdministrationService.AssignPairsAsync(assignments, req.StartAtUtc, req.EndAtUtc);
        return Ok(new { message = $"Successfully assigned {assigned} tests." });
    }
}

public class AssignBatchRequest
{
    public List<string> TestIds { get; set; } = new();
    public List<string> UserIds { get; set; } = new();
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
}

