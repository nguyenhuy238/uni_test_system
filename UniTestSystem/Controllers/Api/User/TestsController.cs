using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers.Api.User;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/user/tests")]
public class TestsController : ControllerBase
{
    private readonly IUserTestService _userTestService;

    public TestsController(IUserTestService userTestService)
    {
        _userTestService = userTestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableTests()
    {
        var tests = await _userTestService.GetAvailablePublishedTestsAsync();
        var availableTests = tests.Where(t => t.IsPublished && t.Type == TestType.Test)
                                  .Select(t => new {
                                      t.Id,
                                      t.Title,
                                      t.DurationMinutes,
                                      t.Type,
                                      t.TotalMaxScore
                                  }).ToList();
        return Ok(availableTests);
    }

    [HttpGet("Tests")]
    public async Task<IActionResult> GetAvailableTestsByRoute()
    {
        var tests = await _userTestService.GetAvailablePublishedTestsAsync();
        var availableTests = tests.Where(t => t.IsPublished && t.Type == TestType.Test)
                                    .Select(t => new {
                                        t.Id,
                                        t.Title,
                                        t.DurationMinutes,
                                        t.Type
                                    }).ToList();
        return Ok(availableTests);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestById(string id)
    {
        var test = await _userTestService.GetPublishedTestByIdAsync(id);
        if (test == null)
            return NotFound();

        return Ok(new {
            test.Id,
            test.Title,
            test.DurationMinutes,
            test.Type,
            test.TotalMaxScore,
            test.ShuffleQuestions
        });
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartTest(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = await _userTestService.StartOrResumeSessionAsync(id, userId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(sessionId)) return NotFound();
        return Ok(new { sessionId });
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetUserResults()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return Ok(await _userTestService.GetUserResultsAsync(userId));
    }

    [HttpGet("results/{id}")]
    public async Task<IActionResult> GetResultById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var result = await _userTestService.GetUserResultByIdAsync(userId, id);
        return result == null ? NotFound() : Ok(result);
    }
}

