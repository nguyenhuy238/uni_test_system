using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application;

public class QuestionService : IQuestionService
{
    private readonly IRepository<Question> _qRepo;
    private readonly IRepository<Test> _tRepo;
    private readonly IRepository<AuditEntry> _auditRepo;
    private readonly IAuditService _audit;

    public QuestionService(IRepository<Question> qRepo, IRepository<Test> tRepo, IRepository<AuditEntry> auditRepo, IAuditService audit)
    {
        _qRepo = qRepo;
        _tRepo = tRepo;
        _auditRepo = auditRepo;
        _audit = audit;
    }

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
        var duplicate = await DetectAdvancedDuplicateAsync(q, null);
        if (duplicate.IsDuplicate)
            throw new ValidationException($"Possible duplicate question detected (ID: {duplicate.MatchedQuestionId}, similarity: {duplicate.Similarity:P0}).");

        ValidateQuestion(q, isNew: true);
        q.CreatedBy = actor; q.CreatedAt = DateTime.UtcNow;
        await _qRepo.InsertAsync(q);
        await _audit.LogAsync(actor, "Question.Create", "Question", q.Id, before: null, after: q);
        return q.Id;
    }

    public async Task<(bool Success, string? Reason)> UpdateAsync(Question q, string actor)
    {
        var tests = await _tRepo.GetAllAsync();
        var usedByPublished = tests.Any(t => t.IsPublished && t.TestQuestions.Any(tq => tq.QuestionId == q.Id));
        if (usedByPublished)
        {
            // NEW: Instead of blocking, we could create a NEW VERSION.
            // But for now, let's keep it simple: if the user wants to save as new version, we clone.
            // If they just click save and it's published, we might want to warn or auto-create a draft.
            return (false, "Question is used in a Published Test. Please use 'Clone' to create a new version.");
        }

        var before = await GetAsync(q.Id);
        if (before == null) return (false, "Not found");

        var duplicate = await DetectAdvancedDuplicateAsync(q, q.Id);
        if (duplicate.IsDuplicate)
            return (false, $"Possible duplicate with Question {duplicate.MatchedQuestionId} (similarity: {duplicate.Similarity:P0}).");

        ValidateQuestion(q, isNew: false);
        q.UpdatedBy = actor; q.UpdatedAt = DateTime.UtcNow;
        
        // Reset status to Draft if it was Rejected or Approved (needs re-approval after major edit)
        if (q.Status == QuestionStatus.Approved || q.Status == QuestionStatus.Rejected)
            q.Status = QuestionStatus.Draft;

        await _qRepo.UpsertAsync(x => x.Id == q.Id, q);
        await _audit.LogAsync(actor, "Question.Update", "Question", q.Id, before, q);
        return (true, null);
    }

    public async Task<(bool Success, string? Reason)> RestoreVersionAsync(string id, int auditId, string actor)
    {
        var current = await GetAsync(id);
        if (current == null) return (false, "Question not found");

        var audit = await _auditRepo.FirstOrDefaultAsync(x => x.Id == auditId && x.EntityName == "Question" && x.EntityId == id);
        if (audit == null) return (false, "Audit version not found");

        var snapshot = DeserializeQuestionSnapshot(audit.After) ?? DeserializeQuestionSnapshot(audit.Before);
        if (snapshot == null) return (false, "Selected audit entry does not contain a restorable question snapshot");

        // Keep stable identity; restore content fields from selected snapshot.
        snapshot.Id = current.Id;
        snapshot.CreatedAt = current.CreatedAt;
        snapshot.CreatedBy = current.CreatedBy;
        snapshot.UpdatedAt = DateTime.UtcNow;
        snapshot.UpdatedBy = actor;

        var (ok, reason) = await UpdateAsync(snapshot, actor);
        if (!ok) return (false, reason ?? "Restore failed");

        await _audit.LogAsync(actor, "Question.RestoreVersion", "Question", id, current, snapshot);
        return (true, null);
    }

    public async Task<(bool Success, string? Reason)> SubmitAsync(string id, string actor)
    {
        var q = await GetAsync(id);
        if (q == null) return (false, "Not found");
        if (q.Status != QuestionStatus.Draft) return (false, "Only Draft can be submitted");
        
        q.Status = QuestionStatus.Pending;
        await _qRepo.UpsertAsync(x => x.Id == id, q);
        await _audit.LogAsync(actor, "Question.Submit", "Question", id, before: null, after: q);
        return (true, null);
    }

    public async Task<(bool Success, string? Reason)> ApproveAsync(string id, string actor)
    {
        var q = await GetAsync(id);
        if (q == null) return (false, "Not found");
        if (q.Status != QuestionStatus.Pending) return (false, "Only Pending can be approved");

        q.Status = QuestionStatus.Approved;
        await _qRepo.UpsertAsync(x => x.Id == id, q);
        await _audit.LogAsync(actor, "Question.Approve", "Question", id, before: null, after: q);
        return (true, null);
    }

    public async Task<(bool Success, string? Reason)> RejectAsync(string id, string actor, string? reason)
    {
        var q = await GetAsync(id);
        if (q == null) return (false, "Not found");
        if (q.Status != QuestionStatus.Pending) return (false, "Only Pending can be rejected");

        q.Status = QuestionStatus.Rejected;
        await _qRepo.UpsertAsync(x => x.Id == id, q);
        await _audit.LogAsync(actor, "Question.Reject", "Question", id, before: null, after: q);
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
        await _audit.LogAsync(actor, "Question.Delete", "Question", id, before, after: null);
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
        await _audit.LogAsync(actor, "Question.Clone", "Question", copy.Id, before: null, after: copy);
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

    private async Task<(bool IsDuplicate, string? MatchedQuestionId, decimal Similarity)> DetectAdvancedDuplicateAsync(Question q, string? excludeId)
    {
        var candidates = await _qRepo.GetAllAsync(x =>
            x.Id != excludeId &&
            x.Type == q.Type &&
            x.SubjectId == q.SubjectId);

        var content = q.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content) || candidates.Count == 0)
            return (false, null, 0m);

        const decimal threshold = 0.90m;
        var sourceTokens = Tokenize(content);
        if (sourceTokens.Count == 0) return (false, null, 0m);

        string? bestId = null;
        decimal bestScore = 0m;

        foreach (var candidate in candidates)
        {
            var score = ComputeJaccard(sourceTokens, Tokenize(candidate.Content ?? string.Empty));
            if (score > bestScore)
            {
                bestScore = score;
                bestId = candidate.Id;
            }
        }

        return (bestScore >= threshold, bestId, bestScore);
    }

    private static HashSet<string> Tokenize(string input)
    {
        var normalized = new string(input
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray());

        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static decimal ComputeJaccard(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0) return 0m;
        var intersection = left.Intersect(right, StringComparer.Ordinal).Count();
        var union = left.Union(right, StringComparer.Ordinal).Count();
        if (union == 0) return 0m;
        return (decimal)intersection / union;
    }

    private static Question? DeserializeQuestionSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var q = JsonSerializer.Deserialize<Question>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (q == null) return null;

            q.Tags ??= new List<string>();
            q.Options ??= new List<Option>();
            q.MatchingPairs ??= new List<MatchPair>();
            q.Media ??= new List<MediaFile>();
            return q;
        }
        catch
        {
            return null;
        }
    }
}
