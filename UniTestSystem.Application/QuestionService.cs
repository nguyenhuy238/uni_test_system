using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application;

public class QuestionService : IQuestionService
{
    private readonly IRepository<Question> _qRepo;
    private readonly IRepository<QuestionBank> _questionBankRepo;
    private readonly IRepository<Option> _optionRepo;
    private readonly IRepository<Test> _tRepo;
    private readonly IRepository<AuditEntry> _auditRepo;
    private readonly IAuditService _audit;

    public QuestionService(
        IRepository<Question> qRepo,
        IRepository<QuestionBank> questionBankRepo,
        IRepository<Option> optionRepo,
        IRepository<Test> tRepo,
        IRepository<AuditEntry> auditRepo,
        IAuditService audit)
    {
        _qRepo = qRepo;
        _questionBankRepo = questionBankRepo;
        _optionRepo = optionRepo;
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
        if (f.Status.HasValue) data = data.Where(x => x.Status == f.Status.Value).ToList();
        if (!string.IsNullOrWhiteSpace(f.SubjectId)) data = data.Where(x => x.SubjectId == f.SubjectId).ToList();
        if (!string.IsNullOrWhiteSpace(f.CourseId))
        {
            var bankIds = (await _questionBankRepo.GetAllAsync(x => x.CourseId == f.CourseId && !x.IsDeleted))
                .Select(x => x.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            data = data.Where(x => !string.IsNullOrWhiteSpace(x.QuestionBankId) && bankIds.Contains(x.QuestionBankId)).ToList();
        }
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

    public Task<List<Question>> GetAllAsync()
    {
        var spec = new Specification<Question>()
            .Include(q => q.Options);
        return _qRepo.ListAsync(spec);
    }

    public Task<Question?> GetAsync(string id)
    {
        var spec = new Specification<Question>(x => x.Id == id)
            .Include(x => x.Options);
        return _qRepo.FirstOrDefaultAsync(spec);
    }

    public async Task<QuestionEditDataResult?> GetEditDataAsync(string id, int take = 20)
    {
        var question = await GetAsync(id);
        if (question == null) return null;

        var versions = (await _auditRepo.GetAllAsync(a =>
                a.EntityName == "Question" &&
                a.EntityId == id &&
                (a.After != null || a.Before != null)))
            .OrderByDescending(a => a.At)
            .Take(take <= 0 ? 20 : take)
            .ToList();

        return new QuestionEditDataResult
        {
            Question = question,
            Versions = versions
        };
    }

    public async Task<string> CreateFromFormAsync(QuestionFormCommand command, string actor)
    {
        var question = command.Question;
        NormalizeQuestionFieldsFromForm(
            question,
            command.Options,
            command.CorrectKeys,
            command.MatchingPairsRaw,
            command.DragTokens,
            command.DragSlotsRaw,
            command.TagsCsv);

        if (command.NewMedia?.Any() == true)
            question.Media = command.NewMedia.ToList();

        return await CreateAsync(question, actor);
    }

    public async Task<(bool Success, string? Reason)> UpdateFromFormAsync(QuestionFormCommand command, string actor)
    {
        var question = command.Question;
        if (string.IsNullOrWhiteSpace(question.Id))
            question.Id = command.Id ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question.Id))
            return (false, "Not found");

        var original = await GetAsync(question.Id);
        if (original == null)
            return (false, "Not found");

        NormalizeQuestionFieldsFromForm(
            question,
            command.Options,
            command.CorrectKeys,
            command.MatchingPairsRaw,
            command.DragTokens,
            command.DragSlotsRaw,
            command.TagsCsv);

        question.CreatedAt = original.CreatedAt;
        question.CreatedBy = original.CreatedBy;

        var mergedMedia = (original.Media ?? new List<MediaFile>()).ToList();
        if (command.NewMedia?.Any() == true)
        {
            var exists = new HashSet<string>(mergedMedia.Select(m => m.Url), StringComparer.OrdinalIgnoreCase);
            foreach (var media in command.NewMedia)
            {
                if (exists.Add(media.Url))
                    mergedMedia.Add(media);
            }
        }

        question.Media = mergedMedia;
        return await UpdateAsync(question, actor);
    }

    public async Task<string> CreateAsync(Question q, string actor)
    {
        var duplicate = await DetectAdvancedDuplicateAsync(q, null);
        if (duplicate.IsDuplicate)
            throw new ValidationException($"Possible duplicate question detected (ID: {duplicate.MatchedQuestionId}, similarity: {duplicate.Similarity:P0}).");

        ValidateQuestion(q, isNew: true);
        AssignOptionQuestionIds(q);
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
        AssignOptionQuestionIds(q);
        q.UpdatedBy = actor; q.UpdatedAt = DateTime.UtcNow;
        
        // Reset status to Draft if it was Rejected or Approved (needs re-approval after major edit)
        if (q.Status == QuestionStatus.Approved || q.Status == QuestionStatus.Rejected)
            q.Status = QuestionStatus.Draft;

        await _qRepo.UpsertAsync(x => x.Id == q.Id, q);
        await _optionRepo.DeleteAsync(o => o.QuestionId == q.Id);
        if (q.Options != null && q.Options.Count > 0)
        {
            foreach (var option in q.Options)
            {
                option.QuestionId = q.Id;
                await _optionRepo.InsertAsync(option);
            }
        }

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

        AssignOptionQuestionIds(copy);
        await _qRepo.InsertAsync(copy);
        await _audit.LogAsync(actor, "Question.Clone", "Question", copy.Id, before: null, after: copy);
        return copy.Id;
    }

    public async Task<RemoveQuestionMediaResult> RemoveMediaAsync(string questionId, string mediaId, string actor)
    {
        var question = await GetAsync(questionId);
        if (question == null)
        {
            return new RemoveQuestionMediaResult
            {
                Success = false,
                Reason = "Not found"
            };
        }

        var media = question.Media.FirstOrDefault(x => x.Id == mediaId);
        if (media == null)
        {
            return new RemoveQuestionMediaResult
            {
                Success = true
            };
        }

        question.Media.RemoveAll(x => x.Id == mediaId);
        var (ok, reason) = await UpdateAsync(question, actor);
        return new RemoveQuestionMediaResult
        {
            Success = ok,
            Reason = reason,
            MediaUrl = ok ? media.Url : null
        };
    }

    public async Task<QuestionDuplicateCheckResult> CheckDuplicateAsync(QuestionDuplicateCheckQuery query)
    {
        var (isDuplicate, matchedQuestionId, similarity) =
            await DetectAdvancedDuplicateAsync(query.Question, query.ExcludeQuestionId);

        return new QuestionDuplicateCheckResult
        {
            IsDuplicate = isDuplicate,
            MatchedQuestionId = matchedQuestionId,
            Similarity = similarity
        };
    }

    private static void NormalizeQuestionFieldsFromForm(
        Question q,
        List<string>? optionsRaw,
        string? correctKeys,
        string? matchingPairsRaw,
        string? dragTokens,
        string? dragSlotsRaw,
        string? tagsCsv)
    {
        var correctSet = string.IsNullOrWhiteSpace(correctKeys)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : correctKeys.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (q.Type == QType.MCQ || q.Type == QType.TrueFalse)
        {
            var rawStrings = q.Type == QType.TrueFalse
                ? new List<string> { "True", "False" }
                : (optionsRaw ?? new List<string>())
                    .SelectMany(line => (line ?? string.Empty).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

            q.Options = rawStrings
                .Select(s => new Option
                {
                    Content = s,
                    IsCorrect = correctSet.Contains(s)
                })
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(tagsCsv))
        {
            q.Tags = tagsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        if (q.Type == QType.Matching)
        {
            q.MatchingPairs = new();
            var lines = (matchingPairsRaw ?? string.Empty).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var parts = raw.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                    q.MatchingPairs.Add(new MatchPair(parts[0], parts[1]));
            }
        }

        if (q.Type == QType.DragDrop)
        {
            var tokens = string.IsNullOrWhiteSpace(dragTokens)
                ? new List<string>()
                : dragTokens.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var slots = new List<DragSlot>();
            var lines = (dragSlotsRaw ?? string.Empty).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var parts = raw.Split('=', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    slots.Add(new DragSlot(parts[0], parts[1]));
            }

            q.DragDrop = new DragDropConfig
            {
                Tokens = tokens,
                Slots = slots
            };
        }
    }

    private static void AssignOptionQuestionIds(Question q)
    {
        if (q.Options == null)
            return;

        foreach (var option in q.Options)
        {
            option.QuestionId = q.Id;
            if (string.IsNullOrWhiteSpace(option.Id))
                option.Id = Guid.NewGuid().ToString("N");
        }
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
