using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/admin/questions")]
public class QuestionsController : ControllerBase
{
    private readonly IRepository<Question> _questions;
    private readonly IRepository<Option> _options;

    public QuestionsController(IRepository<Question> questions, IRepository<Option> options)
    {
        _questions = questions;
        _options = options;
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
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request)
    {
        var question = new Question
        {
            Content = request.Content,
            Type = request.Type,
            Skill = request.Skill,
            Difficulty = request.Difficulty ?? "Junior"
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
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionRequest request)
    {
        var existingQuestion = await _questions.FirstOrDefaultAsync(x => x.Id == id);
        if (existingQuestion == null) return NotFound();

        existingQuestion.Content = request.Content ?? existingQuestion.Content;
        existingQuestion.Type = request.Type ?? existingQuestion.Type;
        existingQuestion.Skill = request.Skill ?? existingQuestion.Skill;
        existingQuestion.Difficulty = request.Difficulty ?? existingQuestion.Difficulty;

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
    public string Skill { get; set; } = "";
    public string? Difficulty { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class UpdateQuestionRequest
{
    public string? Content { get; set; }
    public QType? Type { get; set; }
    public string? Skill { get; set; }
    public string? Difficulty { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class OptionRequest
{
    public string Content { get; set; } = "";
    public bool IsCorrect { get; set; }
}
