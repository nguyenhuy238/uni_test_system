using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface ITestAdministrationService
{
    Task<List<Test>> GetAllTestsAsync();
    Task<List<Test>> GetTestsByStatusAsync(string? status);
    Task<Test?> GetTestByIdAsync(string id);
    Task CreateRawAsync(Test test);
    Task<bool> UpdateRawAsync(string id, Test test);
    Task<bool> DeleteRawAsync(string id);
    Task<List<string>> GetDepartmentOptionsAsync();
    Task<int> AssignPairsAsync(IReadOnlyCollection<TestUserAssignment> assignments, DateTime? startAt = null, DateTime? endAt = null);
    Task CreateAndPublishAsync(Test test, IReadOnlyCollection<string>? selectedQuestionIds);
    Task<bool> UpdateAsync(TestUpdateRequest request, IReadOnlyCollection<string>? selectedQuestionIds);
    Task<bool> DeleteAsync(string id);
    Task<(bool Found, string Message)> ToggleStatusAsync(string id);
    Task<bool> CloneAsync(string id, string createdBy);
    Task<bool> ArchiveAsync(string id);
    Task<TestAssignData?> GetAssignDataAsync(string testId, string? faculty);
    Task<(bool Found, string Message)> AssignToUserAsync(string testId, string userId, DateTime? startAt = null, DateTime? endAt = null);
    Task<(bool Found, string Message)> AssignUsersAsync(string testId, IReadOnlyCollection<string>? userIds, DateTime? startAt = null, DateTime? endAt = null);
    Task<(bool Found, string Message)> AssignByFacultyAsync(string testId, string faculty, DateTime? startAt = null, DateTime? endAt = null);
    Task<string> BulkAssignAsync(IReadOnlyCollection<string>? testIds, string userId, DateTime? startAt = null, DateTime? endAt = null);
    Task<string> BulkAssignAutoAsync(IReadOnlyCollection<string>? testIds, DateTime? startAt = null, DateTime? endAt = null);
}

public sealed class TestUserAssignment
{
    public string TestId { get; set; } = "";
    public string UserId { get; set; } = "";
}

public sealed class TestUpdateRequest
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? CourseId { get; set; }
    public int DurationMinutes { get; set; }
    public int PassScore { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public string SubjectIdFilter { get; set; } = "";
    public int RandomMCQ { get; set; }
    public int RandomTF { get; set; }
    public int RandomEssay { get; set; }
}

public sealed class TestAssignData
{
    public string TestId { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public List<User> Users { get; set; } = new();
    public HashSet<string> AssignedUserIds { get; set; } = new(StringComparer.Ordinal);
    public List<string> Faculties { get; set; } = new();
    public string? SelectedFaculty { get; set; }
}
