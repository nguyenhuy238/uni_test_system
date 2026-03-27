using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Question_View)]
[Route("api/admin/questions")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _svc;
    private readonly IRepository<Subject> _subjectRepo;
    private readonly IRepository<DifficultyLevel> _difficultyLevelRepo;
    private readonly IRepository<Skill> _skillRepo;
    private readonly IRepository<QuestionBank> _questionBankRepo;

    public QuestionsController(
        IQuestionService svc,
        IRepository<Subject> subjectRepo,
        IRepository<DifficultyLevel> difficultyLevelRepo,
        IRepository<Skill> skillRepo,
        IRepository<QuestionBank> questionBankRepo)
    {
        _svc = svc;
        _subjectRepo = subjectRepo;
        _difficultyLevelRepo = difficultyLevelRepo;
        _skillRepo = skillRepo;
        _questionBankRepo = questionBankRepo;
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
        var subjectById = (await _subjectRepo.GetAllAsync(x => !x.IsDeleted))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var difficultyById = (await _difficultyLevelRepo.GetAllAsync(x => !x.IsDeleted))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        var skillById = (await _skillRepo.GetAllAsync(x => !x.IsDeleted))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        var items = questions.Select(q => new
        {
            q.Id,
            q.Content,
            q.Type,
            q.Status,
            q.Options,
            q.MatchingPairs,
            q.DragDrop,
            q.SkillId,
            q.DifficultyLevelId,
            q.SubjectId,
            skill = ResolveDisplayName(skillById, q.SkillId),
            difficulty = ResolveDisplayName(difficultyById, q.DifficultyLevelId),
            subject = ResolveDisplayName(subjectById, q.SubjectId),
            mediaUrl = q.Media?.FirstOrDefault()?.Url,
            q.CreatedAt
        });

        return Ok(items);
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMetadata()
    {
        var skills = (await _skillRepo.GetAllAsync(x => !x.IsDeleted))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            });

        var difficulties = (await _difficultyLevelRepo.GetAllAsync(x => !x.IsDeleted))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            });

        var subjects = (await _subjectRepo.GetAllAsync(x => !x.IsDeleted))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            });

        var questionBanks = (await _questionBankRepo.GetAllAsync(x => !x.IsDeleted))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            });

        return Ok(new
        {
            skills,
            difficulties,
            subjects,
            questionBanks
        });
    }

    private static string ResolveDisplayName(IReadOnlyDictionary<string, string> map, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        return map.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : id;
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

    [HttpGet("by-course/{courseId}")]
    public async Task<IActionResult> GetApprovedByCourse(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return BadRequest(new { message = "courseId is required" });

        var paged = await _svc.SearchAsync(new QuestionFilter
        {
            CourseId = courseId,
            Status = QuestionStatus.Approved,
            Page = 1,
            PageSize = int.MaxValue,
            Sort = "CreatedAt_desc"
        });

        var items = paged.Items.Select(q => new
        {
            q.Id,
            q.Content,
            q.Type,
            q.SubjectId,
            q.DifficultyLevelId,
            q.Tags,
            q.Status,
            q.QuestionBankId
        });

        return Ok(new
        {
            CourseId = courseId,
            Total = paged.Total,
            Items = items
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Question_Create)]
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QuestionBankId))
            return BadRequest(new { message = "QuestionBankId is required" });

        var question = new Question
        {
            Content = request.Content,
            Type = request.Type,
            QuestionBankId = request.QuestionBankId,
            SubjectId = request.SubjectId,
            DifficultyLevelId = request.DifficultyLevelId ?? "Easy",
            SkillId = request.SkillId,
            MatchingPairs = request.MatchingPairs ?? new List<MatchPair>(),
            DragDrop = request.DragDrop,
            Options = (request.Options ?? new List<OptionRequest>())
                .Select(optionRequest => new Option
                {
                    Content = optionRequest.Content,
                    IsCorrect = optionRequest.IsCorrect
                })
                .ToList()
        };

        var actor = User.Identity?.Name ?? "admin";
        string id;
        try
        {
            id = await _svc.CreateAsync(question, actor);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var created = await _svc.GetAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, created ?? question);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Question_Edit)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionRequest request)
    {
        var existingQuestion = await _svc.GetAsync(id);
        if (existingQuestion == null) return NotFound();

        // Validate the request
        if (request.SubjectId != null && string.IsNullOrWhiteSpace(request.SubjectId))
            return BadRequest(new { message = "SubjectId cannot be empty if provided" });
        if (request.QuestionBankId != null && string.IsNullOrWhiteSpace(request.QuestionBankId))
            return BadRequest(new { message = "QuestionBankId cannot be empty if provided" });

        // Apply updates
        existingQuestion.Content = request.Content ?? existingQuestion.Content;
        existingQuestion.Type = request.Type ?? existingQuestion.Type;
        existingQuestion.SubjectId = request.SubjectId ?? existingQuestion.SubjectId;
        existingQuestion.QuestionBankId = request.QuestionBankId ?? existingQuestion.QuestionBankId;
        existingQuestion.DifficultyLevelId = request.DifficultyLevelId ?? existingQuestion.DifficultyLevelId;
        if (request.SkillId != null)
        {
            existingQuestion.SkillId = request.SkillId;
        }
        if (request.MatchingPairs != null)
        {
            existingQuestion.MatchingPairs = request.MatchingPairs;
        }
        if (request.DragDrop != null)
        {
            existingQuestion.DragDrop = request.DragDrop;
        }
        
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
    public string QuestionBankId { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string? DifficultyLevelId { get; set; }
    public string? SkillId { get; set; }
    public List<MatchPair>? MatchingPairs { get; set; }
    public DragDropConfig? DragDrop { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class UpdateQuestionRequest
{
    public string? Content { get; set; }
    public QType? Type { get; set; }
    public string? QuestionBankId { get; set; }
    public string? SubjectId { get; set; }
    public string? DifficultyLevelId { get; set; }
    public string? SkillId { get; set; }
    public List<MatchPair>? MatchingPairs { get; set; }
    public DragDropConfig? DragDrop { get; set; }
    public List<OptionRequest>? Options { get; set; }
}

public class OptionRequest
{
    public string Content { get; set; } = "";
    public bool IsCorrect { get; set; }
}
