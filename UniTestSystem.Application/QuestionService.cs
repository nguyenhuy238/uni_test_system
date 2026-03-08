using System.ComponentModel.DataAnnotations;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application;

public class QuestionService : IQuestionService
{
    private readonly IRepository<Question> _qRepo;
    private readonly IRepository<Test> _tRepo;
    private readonly IAuditService _audit;

    public QuestionService(IRepository<Question> qRepo, IRepository<Test> tRepo, IAuditService audit)
    { _qRepo = qRepo; _tRepo = tRepo; _audit = audit; }

    public async Task<PagedResult<Question>> SearchAsync(QuestionFilter f)
    {
        var data = await _qRepo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(f.Keyword))
            data = data.Where(x => x.Content.Contains(f.Keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        if (f.Type.HasValue) data = data.Where(x => x.Type == f.Type.Value).ToList();
        if (!string.IsNullOrWhiteSpace(f.SubjectId)) data = data.Where(x => x.SubjectId == f.SubjectId).ToList();
        if (!string.IsNullOrWhiteSpace(f.DifficultyLevelId)) data = data.Where(x => x.DifficultyLevelId == f.DifficultyLevelId).ToList();
        if (!string.IsNullOrWhiteSpace(f.TagsCsv))
        {
            var tags = f.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            data = data.Where(x => x.Tags.Any(t => tags.Contains(t))).ToList();
        }
        if (f.From.HasValue) data = data.Where(x => x.CreatedAt >= f.From.Value).ToList();
        if (f.To.HasValue) data = data.Where(x => x.CreatedAt <= f.To.Value).ToList();

        data = f.Sort switch
        {
            "Content_asc" => data.OrderBy(x => x.Content).ToList(),
            "Content_desc" => data.OrderByDescending(x => x.Content).ToList(),
            "CreatedAt_asc" => data.OrderBy(x => x.CreatedAt).ToList(),
            _ => data.OrderByDescending(x => x.CreatedAt).ToList()
        };

        var total = data.Count;
        var items = data.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToList();
        return new PagedResult<Question> { Page = f.Page, PageSize = f.PageSize, Total = total, Items = items };
    }

    public Task<Question?> GetAsync(string id) => _qRepo.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<string> CreateAsync(Question q, string actor)
    {
        ValidateQuestion(q, isNew: true);
        q.CreatedBy = actor; q.CreatedAt = DateTime.UtcNow;
        await _qRepo.InsertAsync(q);
        await _audit.LogAsync(actor, "Question.Create", q.Id, before: null, after: q);
        return q.Id;
    }

    public async Task<(bool Success, string? Reason)> UpdateAsync(Question q, string actor)
    {
        var tests = await _tRepo.GetAllAsync();
        var usedByPublished = tests.Any(t => t.IsPublished && t.TestQuestions.Any(tq => tq.QuestionId == q.Id));
        if (usedByPublished)
            return (false, "Question is used in a Published Test. Only cloning or minor edits allowed.");

        var before = await GetAsync(q.Id);
        if (before == null) return (false, "Not found");

        ValidateQuestion(q, isNew: false);
        q.UpdatedBy = actor; q.UpdatedAt = DateTime.UtcNow;
        await _qRepo.UpsertAsync(x => x.Id == q.Id, q);
        await _audit.LogAsync(actor, "Question.Update", q.Id, before, q);
        return (true, null);
    }

    public async Task<(bool Success, string? Reason)> DeleteAsync(string id, string actor)
    {
        var tests = await _tRepo.GetAllAsync();
        if (tests.Any(t => t.IsPublished && t.TestQuestions.Any(tq => tq.QuestionId == id)))
            return (false, "Question is used in a Published Test. Delete is blocked.");

        var before = await GetAsync(id);
        if (before == null) return (false, "Not found");

        await _qRepo.DeleteAsync(x => x.Id == id);
        await _audit.LogAsync(actor, "Question.Delete", id, before, after: null);
        return (true, null);
    }

    public async Task<string> CloneAsync(string id, string actor)
    {
        var q = await GetAsync(id) ?? throw new Exception("Not found");
        var copy = new Question
        {
            Id = Guid.NewGuid().ToString("N"),
            Content = "[CLONE] " + q.Content,
            Type = q.Type,
            SubjectId = q.SubjectId,
            DifficultyLevelId = q.DifficultyLevelId,
            SkillId = q.SkillId,
            Tags = new List<string>(q.Tags),
            Media = q.Media.Select(m => new MediaFile
            {
                FileName = m.FileName,
                Url = m.Url,
                ContentType = m.ContentType,
                Size = m.Size,
                Caption = m.Caption
            }).ToList(),
            MatchingPairs = q.MatchingPairs != null ? new List<MatchPair>(q.MatchingPairs) : new(),
            DragDrop = q.DragDrop != null ? new DragDropConfig { Tokens = new List<string>(q.DragDrop.Tokens), Slots = q.DragDrop.Slots != null ? new List<DragSlot>(q.DragDrop.Slots) : new() } : null,
            Options = q.Options.Select(o => new Option
            {
                Content = o.Content,
                IsCorrect = o.IsCorrect
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actor
        };

        await _qRepo.InsertAsync(copy);
        await _audit.LogAsync(actor, "Question.Clone", copy.Id, before: null, after: copy);
        return copy.Id;
    }

    private static void ValidateQuestion(Question q, bool isNew)
    {
        if (string.IsNullOrWhiteSpace(q.Content)) throw new ValidationException("Content required");
        switch (q.Type)
        {
            case QType.MCQ:
                if (q.Options == null || q.Options.Count < 2) throw new ValidationException("MCQ requires >= 2 options");
                if (!q.Options.Any(o => o.IsCorrect)) throw new ValidationException("MCQ requires at least 1 correct option");
                break;
            case QType.TrueFalse:
                if (q.Options == null || q.Options.Count != 2)
                {
                    q.Options = new List<Option>
                    {
                        new Option { Content = "True", IsCorrect = false },
                        new Option { Content = "False", IsCorrect = false }
                    };
                }
                if (q.Options.Count(o => o.IsCorrect) != 1)
                    throw new ValidationException("True/False requires exactly one correct option");
                break;
            case QType.Matching:
                if (q.MatchingPairs == null || q.MatchingPairs.Count == 0)
                    throw new ValidationException("Matching requires at least 1 pair (Left|Right)");
                break;
            case QType.DragDrop:
                if (q.DragDrop == null || (q.DragDrop.Slots?.Count ?? 0) == 0 || (q.DragDrop.Tokens?.Count ?? 0) == 0)
                    throw new ValidationException("DragDrop requires tokens and slots (Name=Answer)");
                break;
            case QType.Essay:
                break;
        }
    }
}
