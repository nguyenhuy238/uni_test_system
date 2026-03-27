using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Question_View)]
[Route("api/admin/questions")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _svc;

    public QuestionsController(IQuestionService svc)
    {
        _svc = svc;
    }

    [HttpPost("submit/{id}")]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Submit(string id)
    {
        var (success, reason) = await _svc.SubmitAsync(id, User.Identity?.Name ?? "admin");
        if (!success) return BadRequest(new { message = reason ?? "Submit failed" });
        return Ok();
    }

    [HttpPost("approve/{id}")]
    [Authorize(Policy = PermissionCodes.Question_Approve)]
    public async Task<IActionResult> Approve(string id)
    {
        var (success, reason) = await _svc.ApproveAsync(id, User.Identity?.Name ?? "admin");
        if (!success) return BadRequest(new { message = reason ?? "Approve failed" });
        return Ok();
    }

    [HttpPost("reject/{id}")]
    [Authorize(Policy = PermissionCodes.Question_Approve)]
    public async Task<IActionResult> Reject(string id, [FromQuery] string? reason)
    {
        var (success, r) = await _svc.RejectAsync(id, User.Identity?.Name ?? "admin", reason);
        if (!success) return BadRequest(new { message = r ?? "Reject failed" });
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var questions = await _svc.GetAllAsync();
        return Ok(questions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var question = await _svc.GetAsync(id);
        if (question == null) return NotFound();

        return Ok(new {
            question,
            options = question.Options ?? new List<Option>()
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Create)]
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request)
    {
        var question = new Question
        {
            Content = request.Content,
            Type = request.Type,
            SubjectId = request.SubjectId,
            DifficultyLevelId = request.DifficultyLevelId ?? "Easy",
            Options = (request.Options ?? new List<OptionRequest>())
                .Select(optionRequest => new Option
                {
                    Content = optionRequest.Content,
                    IsCorrect = optionRequest.IsCorrect
                })
                .ToList()
        };

        var actor = User.Identity?.Name ?? "admin";
        var id = await _svc.CreateAsync(question, actor);
        var created = await _svc.GetAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, created ?? question);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionRequest request)
    {
        var existingQuestion = await _svc.GetAsync(id);
        if (existingQuestion == null) return NotFound();

        existingQuestion.Content = request.Content ?? existingQuestion.Content;
        existingQuestion.Type = request.Type ?? existingQuestion.Type;
        existingQuestion.SubjectId = request.SubjectId ?? existingQuestion.SubjectId;
        existingQuestion.DifficultyLevelId = request.DifficultyLevelId ?? existingQuestion.DifficultyLevelId;
        if (request.Options != null)
        {
            existingQuestion.Options = request.Options
                .Select(optionRequest => new Option
                {
                    QuestionId = id,
                    Content = optionRequest.Content,
                    IsCorrect = optionRequest.IsCorrect
                })
                .ToList();
        }

        var actor = User.Identity?.Name ?? "admin";
        var (success, reason) = await _svc.UpdateAsync(existingQuestion, actor);
        if (!success) return BadRequest(new { message = reason ?? "Update failed" });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionCodes.Question_Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var actor = User.Identity?.Name ?? "admin";
        var (success, reason) = await _svc.DeleteAsync(id, actor);
        if (!success)
        {
            if (string.Equals(reason, "Not found", StringComparison.OrdinalIgnoreCase))
                return NotFound();
            return BadRequest(new { message = reason ?? "Delete failed" });
        }

        return NoContent();
    }
}

public class CreateQuestionRequest
{
    public string Content { get; set; } = "";
    public QType Type { get; set; }
    public string SubjectId { get; set; } = "";
    public string? DifficultyLevelId { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class UpdateQuestionRequest
{
    public string? Content { get; set; }
    public QType? Type { get; set; }
    public string? SubjectId { get; set; }
    public string? DifficultyLevelId { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class OptionRequest
{
    public string Content { get; set; } = "";
    public bool IsCorrect { get; set; }
}

