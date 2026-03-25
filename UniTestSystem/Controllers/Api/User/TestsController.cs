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
    private readonly IEntityStore<Test> _tests;
    private readonly IEntityStore<Session> _sessions;
    private readonly IEntityStore<Result> _results;

    public TestsController(IEntityStore<Test> tests, IEntityStore<Session> sessions, IEntityStore<Result> results)
    {
        _tests = tests;
        _sessions = sessions;
        _results = results;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableTests()
    {
        var tests = await _tests.GetAllAsync();
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
        var tests = await _tests.GetAllAsync();
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
        var test = await _tests.FirstOrDefaultAsync(x => x.Id == id);
        if (test == null || !test.IsPublished)
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
        var test = await _tests.FirstOrDefaultAsync(x => x.Id == id);
        if (test == null || !test.IsPublished)
            return NotFound();

        // Check if user already has an active session
        var existingSession = await _sessions.FirstOrDefaultAsync(s => s.TestId == id && s.UserId == userId && s.Status == SessionStatus.InProgress);
        if (existingSession != null)
            return Ok(new { sessionId = existingSession.Id });

        // Create new session
        var session = new Session
        {
            Id = Guid.NewGuid().ToString("N"),
            TestId = id,
            UserId = userId ?? "",
            Status = SessionStatus.InProgress,
            StartAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        await _sessions.InsertAsync(session);
        return Ok(new { sessionId = session.Id });
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetUserResults()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var results = await _results.GetAllAsync();
        var userResults = results.Where(r => r.UserId == userId)
                               .Join(await _tests.GetAllAsync(), r => r.TestId, t => t.Id, (r, t) => new {
                                   r.Id,
                                   t.Title,
                                   r.Score,
                                   r.MaxScore,
                                   r.SubmitTime,
                                   r.Status,
                                   t.Type
                               })
                               .OrderByDescending(r => r.SubmitTime)
                               .ToList();

        return Ok(userResults);
    }

    [HttpGet("results/{id}")]
    public async Task<IActionResult> GetResultById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _results.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (result == null)
            return NotFound();

        var test = await _tests.FirstOrDefaultAsync(t => t.Id == result.TestId);
        return Ok(new {
            result.Id,
            Title = test?.Title ?? "Untitled",
            result.Score,
            result.MaxScore,
            result.SubmitTime,
            result.Status,
            Type = test?.Type ?? TestType.Test
        });
    }
}

