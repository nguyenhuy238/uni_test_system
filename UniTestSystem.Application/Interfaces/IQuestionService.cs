using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public class QuestionFilter
    {
        public string? Keyword { get; set; }
        public QType? Type { get; set; }
        public string? SubjectId { get; set; }
        public string? CourseId { get; set; }
        public QuestionStatus? Status { get; set; }
        public string? DifficultyLevelId { get; set; }
        public string? TagsCsv { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; } = "CreatedAt_desc";
    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<T> Items { get; set; } = new();
    }

    public interface IQuestionService
    {
        Task<PagedResult<Question>> SearchAsync(QuestionFilter f);
        Task<List<Question>> GetAllAsync();
        Task<Question?> GetAsync(string id);
        Task<QuestionEditDataResult?> GetEditDataAsync(string id, int take = 20);
        Task<string> CreateFromFormAsync(QuestionFormCommand command, string actor);
        Task<(bool Success, string? Reason)> UpdateFromFormAsync(QuestionFormCommand command, string actor);
        Task<string> CreateAsync(Question q, string actor);
        Task<(bool Success, string? Reason)> UpdateAsync(Question q, string actor);
        Task<(bool Success, string? Reason)> DeleteAsync(string id, string actor);
        Task<string> CloneAsync(string id, string actor);
        Task<RemoveQuestionMediaResult> RemoveMediaAsync(string questionId, string mediaId, string actor);
        Task<QuestionDuplicateCheckResult> CheckDuplicateAsync(QuestionDuplicateCheckQuery query);

        Task<(bool Success, string? Reason)> SubmitAsync(string id, string actor);
        Task<(bool Success, string? Reason)> ApproveAsync(string id, string actor);
        Task<(bool Success, string? Reason)> RejectAsync(string id, string actor, string? reason);
        Task<(bool Success, string? Reason)> RestoreVersionAsync(string id, int auditId, string actor);
    }

    public sealed class QuestionEditDataResult
    {
        public Question Question { get; set; } = new();
        public List<AuditEntry> Versions { get; set; } = new();
    }

    public sealed class QuestionFormCommand
    {
        public string? Id { get; set; }
        public Question Question { get; set; } = new();
        public List<string>? Options { get; set; }
        public string? CorrectKeys { get; set; }
        public string? MatchingPairsRaw { get; set; }
        public string? DragTokens { get; set; }
        public string? DragSlotsRaw { get; set; }
        public string? TagsCsv { get; set; }
        public List<MediaFile>? NewMedia { get; set; }
    }

    public sealed class RemoveQuestionMediaResult
    {
        public bool Success { get; set; }
        public string? Reason { get; set; }
        public string? MediaUrl { get; set; }
    }

    public sealed class QuestionDuplicateCheckQuery
    {
        public Question Question { get; set; } = new();
        public string? ExcludeQuestionId { get; set; }
    }

    public sealed class QuestionDuplicateCheckResult
    {
        public bool IsDuplicate { get; set; }
        public string? MatchedQuestionId { get; set; }
        public decimal Similarity { get; set; }
    }

    

    
}
