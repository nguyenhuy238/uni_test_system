using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api;

[ApiController]
[Authorize(Policy = PermissionCodes.Tests_View)]
[Route("api/admin/tests")]
public class AdminTestsController : ControllerBase
{
    private readonly ITestAdministrationService _testAdministrationService;

    public AdminTestsController(ITestAdministrationService testAdministrationService)
    {
        _testAdministrationService = testAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tests = await _testAdministrationService.GetAllTestsAsync();
        return Ok(tests.Select(MapTestDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var t = await _testAdministrationService.GetTestByIdAsync(id);
        if (t == null) return NotFound();
        return Ok(MapTestDto(t));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Create([FromBody] AdminTestUpsertRequest request)
    {
        var test = request.ToDomain();
        await _testAdministrationService.CreateRawAsync(test);

        var created = await _testAdministrationService.GetTestByIdAsync(test.Id) ?? test;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapTestDto(created));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Update(string id, [FromBody] AdminTestUpsertRequest request)
    {
        var test = request.ToDomain();
        var updated = await _testAdministrationService.UpdateRawAsync(id, test);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _testAdministrationService.DeleteRawAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static AdminTestDto MapTestDto(Test test)
    {
        return new AdminTestDto
        {
            Id = test.Id,
            AssessmentId = test.AssessmentId,
            Title = test.Title,
            Type = test.Type,
            DurationMinutes = test.DurationMinutes,
            AssessmentType = test.AssessmentType,
            PassScore = test.PassScore,
            ShuffleQuestions = test.ShuffleQuestions,
            ShuffleOptions = test.ShuffleOptions,
            TotalMaxScore = test.TotalMaxScore,
            IsPublished = test.IsPublished,
            IsArchived = test.IsArchived,
            CourseId = test.CourseId,
            CreatedBy = test.CreatedBy,
            CreatedAt = test.CreatedAt,
            UpdatedBy = test.UpdatedBy,
            UpdatedAt = test.UpdatedAt,
            PublishedAt = test.PublishedAt,
            TestQuestions = test.TestQuestions
                .Select(x => new AdminTestQuestionItemDto
                {
                    TestId = x.TestId,
                    QuestionId = x.QuestionId,
                    Points = x.Points,
                    Order = x.Order
                })
                .ToList()
        };
    }
}

public class AdminTestDto
{
    public string Id { get; set; } = "";
    public string? AssessmentId { get; set; }
    public string Title { get; set; } = "";
    public TestType Type { get; set; } = TestType.Test;
    public int DurationMinutes { get; set; } = 30;
    public AssessmentType AssessmentType { get; set; } = AssessmentType.Quiz;
    public int PassScore { get; set; } = 5;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public decimal TotalMaxScore { get; set; } = 10m;
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public string? CourseId { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<AdminTestQuestionItemDto> TestQuestions { get; set; } = new();
}

public class AdminTestUpsertRequest
{
    public string? Id { get; set; }
    public string? AssessmentId { get; set; }
    public string Title { get; set; } = "";
    public TestType Type { get; set; } = TestType.Test;
    public int DurationMinutes { get; set; } = 30;
    public AssessmentType AssessmentType { get; set; } = AssessmentType.Quiz;
    public int PassScore { get; set; } = 5;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;
    public decimal TotalMaxScore { get; set; } = 10m;
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public string? CourseId { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<AdminTestQuestionItemDto>? TestQuestions { get; set; }

    public Test ToDomain()
    {
        return new Test
        {
            Id = Id ?? string.Empty,
            AssessmentId = AssessmentId,
            Title = Title,
            Type = Type,
            DurationMinutes = DurationMinutes,
            AssessmentType = AssessmentType,
            PassScore = PassScore,
            ShuffleQuestions = ShuffleQuestions,
            ShuffleOptions = ShuffleOptions,
            TotalMaxScore = TotalMaxScore,
            IsPublished = IsPublished,
            IsArchived = IsArchived,
            CourseId = CourseId,
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            UpdatedBy = UpdatedBy,
            UpdatedAt = UpdatedAt,
            PublishedAt = PublishedAt,
            TestQuestions = (TestQuestions ?? new List<AdminTestQuestionItemDto>())
                .Select(x => new TestQuestion
                {
                    TestId = x.TestId,
                    QuestionId = x.QuestionId,
                    Points = x.Points,
                    Order = x.Order
                })
                .ToList()
        };
    }
}

public class AdminTestQuestionItemDto
{
    public string TestId { get; set; } = "";
    public string QuestionId { get; set; } = "";
    public decimal Points { get; set; } = 1m;
    public int Order { get; set; }
}

