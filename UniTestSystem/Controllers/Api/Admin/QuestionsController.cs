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
    private readonly IRepository<Question> _questions;
    private readonly IRepository<Option> _options;
    private readonly IQuestionService _svc;

    public QuestionsController(IRepository<Question> questions, IRepository<Option> options, IQuestionService svc)
    {
        _questions = questions;
        _options = options;
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
        var questions = await _questions.GetAllAsync();
        return Ok(questions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var question = await _questions.FirstOrDefaultAsync(x => x.Id == id);
        if (question == null) return NotFound();

        var options = await _options.GetAllAsync();
        var questionOptions = options.Where(o => o.QuestionId == id).ToList();

        return Ok(new {
            question,
            options = questionOptions
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
            DifficultyLevelId = request.DifficultyLevelId ?? "Easy"
        };

        await _questions.InsertAsync(question);

        // Add options if MCQ
        if (request.Type == QType.MCQ && request.Options != null)
        {
            foreach (var optionRequest in request.Options)
            {
                var option = new Option
                {
                    QuestionId = question.Id,
                    Content = optionRequest.Content,
                    IsCorrect = optionRequest.IsCorrect
                };
                await _options.InsertAsync(option);
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionRequest request)
    {
        var existingQuestion = await _questions.FirstOrDefaultAsync(x => x.Id == id);
        if (existingQuestion == null) return NotFound();

        existingQuestion.Content = request.Content ?? existingQuestion.Content;
        existingQuestion.Type = request.Type ?? existingQuestion.Type;
        existingQuestion.SubjectId = request.SubjectId ?? existingQuestion.SubjectId;
        existingQuestion.DifficultyLevelId = request.DifficultyLevelId ?? existingQuestion.DifficultyLevelId;

        await _questions.UpsertAsync(x => x.Id == id, existingQuestion);

        // Update options if provided
        if (request.Options != null)
        {
            // Delete existing options
            await _options.DeleteAsync(o => o.QuestionId == id);

            // Add new options
            foreach (var optionRequest in request.Options)
            {
                var option = new Option
                {
                    QuestionId = id,
                    Content = optionRequest.Content,
                    IsCorrect = optionRequest.IsCorrect
                };
                await _options.InsertAsync(option);
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionCodes.Question_Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        await _options.DeleteAsync(o => o.QuestionId == id);
        await _questions.DeleteAsync(x => x.Id == id);
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
