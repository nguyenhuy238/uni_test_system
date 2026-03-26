using System.Text;
using System.Text.RegularExpressions;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class TestAdministrationService : ITestAdministrationService
{
    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<Assessment> _assessmentRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IQuestionService _questionService;
    private readonly INotificationService? _notificationService;

    public TestAdministrationService(
        IRepository<Test> testRepo,
        IRepository<Assessment> assessmentRepo,
        IRepository<Student> studentRepo,
        IQuestionService questionService,
        INotificationService? notificationService = null)
    {
        _testRepo = testRepo;
        _assessmentRepo = assessmentRepo;
        _studentRepo = studentRepo;
        _questionService = questionService;
        _notificationService = notificationService;
    }

    public Task<List<Test>> GetAllTestsAsync()
    {
        return _testRepo.GetAllAsync();
    }

    public async Task<List<Test>> GetTestsByStatusAsync(string? status)
    {
        var tests = await _testRepo.GetAllAsync();

        if (!string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            tests = tests.Where(t => !t.IsArchived).ToList();
        }
        else
        {
            tests = tests.Where(t => t.IsArchived).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                tests = tests.Where(t => !t.IsPublished).ToList();
            }
            else if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                tests = tests.Where(t => t.IsPublished).ToList();
            }
        }

        return tests;
    }

    public Task<Test?> GetTestByIdAsync(string id)
    {
        return _testRepo.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task CreateRawAsync(Test test)
    {
        test.Id = string.IsNullOrWhiteSpace(test.Id) ? Guid.NewGuid().ToString("N") : test.Id;
        if (test.CreatedAt == default) test.CreatedAt = DateTime.UtcNow;
        await _testRepo.InsertAsync(test);
    }

    public async Task<bool> UpdateRawAsync(string id, Test test)
    {
        var existing = await _testRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return false;

        test.Id = id;
        test.CreatedAt = existing.CreatedAt;
        test.UpdatedAt = DateTime.UtcNow;
        await _testRepo.UpsertAsync(x => x.Id == id, test);
        return true;
    }

    public async Task<bool> DeleteRawAsync(string id)
    {
        var existing = await _testRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return false;

        await _testRepo.DeleteAsync(x => x.Id == id);
        return true;
    }

    public async Task<List<string>> GetDepartmentOptionsAsync()
    {
        return (await _studentRepo.GetAllAsync())
            .Select(x => x.StudentClassId ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<int> AssignPairsAsync(IReadOnlyCollection<TestUserAssignment> assignments, DateTime? startAt = null, DateTime? endAt = null)
    {
        var assigned = 0;
        foreach (var item in assignments)
        {
            if (string.IsNullOrWhiteSpace(item.TestId) || string.IsNullOrWhiteSpace(item.UserId))
            {
                continue;
            }

            var (found, _) = await AssignToUserAsync(item.TestId, item.UserId, startAt, endAt);
            if (found) assigned++;
        }

        return assigned;
    }

    public async Task CreateAndPublishAsync(Test test, IReadOnlyCollection<string>? selectedQuestionIds)
    {
        var selectedIds = selectedQuestionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList()
                          ?? new List<string>();

        if (selectedIds.Any())
        {
            test.TestQuestions = selectedIds
                .Select(qid => new TestQuestion { QuestionId = qid })
                .ToList();
            test.RandomMCQ = 0;
            test.RandomTF = 0;
            test.RandomEssay = 0;
        }

        test.IsPublished = true;
        test.CreatedAt = DateTime.UtcNow;
        test.PublishedAt = DateTime.UtcNow;
        test.ShuffleQuestions = true;
        test.ShuffleOptions = true;
        test.FrozenRandom = new FrozenRandomConfig
        {
            SubjectIdFilter = test.SubjectIdFilter,
            RandomMCQ = test.RandomMCQ,
            RandomTF = test.RandomTF,
            RandomEssay = test.RandomEssay
        };

        await _testRepo.InsertAsync(test);

        if (test.TestQuestions.Any())
        {
            await CreateSnapshotsAsync(test);
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
        }
    }

    public async Task<bool> UpdateAsync(TestUpdateRequest request, IReadOnlyCollection<string>? selectedQuestionIds)
    {
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (test == null)
        {
            return false;
        }

        test.Title = request.Title;
        test.DurationMinutes = request.DurationMinutes;
        test.PassScore = request.PassScore;
        test.ShuffleQuestions = request.ShuffleQuestions;
        test.ShuffleOptions = request.ShuffleOptions;
        test.SubjectIdFilter = request.SubjectIdFilter;
        test.RandomMCQ = request.RandomMCQ;
        test.RandomTF = request.RandomTF;
        test.RandomEssay = request.RandomEssay;
        test.UpdatedAt = DateTime.UtcNow;

        var selectedIds = selectedQuestionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList()
                          ?? new List<string>();
        if (selectedIds.Any())
        {
            test.TestQuestions = selectedIds
                .Select(qid => new TestQuestion { TestId = test.Id, QuestionId = qid })
                .ToList();
            test.RandomMCQ = 0;
            test.RandomTF = 0;
            test.RandomEssay = 0;
        }
        else
        {
            test.TestQuestions = new List<TestQuestion>();
        }

        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == id);
        if (test == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(test.AssessmentId))
        {
            await _assessmentRepo.DeleteAsync(a => a.Id == test.AssessmentId);
        }

        await _testRepo.DeleteAsync(t => t.Id == id);
        return true;
    }

    public async Task<(bool Found, string Message)> ToggleStatusAsync(string id)
    {
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }

        test.IsPublished = !test.IsPublished;
        if (test.IsPublished)
        {
            test.PublishedAt = DateTime.UtcNow;
            test.FrozenRandom = new FrozenRandomConfig
            {
                SubjectIdFilter = test.SubjectIdFilter,
                RandomMCQ = test.RandomMCQ,
                RandomTF = test.RandomTF,
                RandomEssay = test.RandomEssay
            };

            if (test.TestQuestions.Any())
            {
                await CreateSnapshotsAsync(test);
            }

            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            return (true, "Đã chuyển sang Published và tạo Snapshot câu hỏi.");
        }

        test.PublishedAt = null;
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
        return (true, "Đã chuyển sang Draft.");
    }

    public async Task<bool> CloneAsync(string id, string createdBy)
    {
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (test == null)
        {
            return false;
        }

        var clone = new Test
        {
            Title = test.Title + " (Clone)",
            DurationMinutes = test.DurationMinutes,
            PassScore = test.PassScore,
            ShuffleQuestions = test.ShuffleQuestions,
            ShuffleOptions = test.ShuffleOptions,
            SubjectIdFilter = test.SubjectIdFilter,
            RandomMCQ = test.RandomMCQ,
            RandomTF = test.RandomTF,
            RandomEssay = test.RandomEssay,
            TotalMaxScore = test.TotalMaxScore,
            CourseId = test.CourseId,
            IsPublished = false,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            TestQuestions = test.TestQuestions
                .Select(q => new TestQuestion { QuestionId = q.QuestionId, Points = q.Points })
                .ToList()
        };

        await _testRepo.InsertAsync(clone);
        return true;
    }

    public async Task<bool> ArchiveAsync(string id)
    {
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (test == null)
        {
            return false;
        }

        test.IsArchived = true;
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
        return true;
    }

    public async Task<TestAssignData?> GetAssignDataAsync(string testId, string? faculty)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId);
        if (test == null)
        {
            return null;
        }

        var allStudents = await _studentRepo.GetAllAsync();
        var classes = allStudents
            .Select(u => u.StudentClassId ?? "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        var usersToShow = string.IsNullOrWhiteSpace(faculty)
            ? allStudents
            : allStudents.Where(u => string.Equals(u.StudentClassId ?? "", faculty, StringComparison.OrdinalIgnoreCase)).ToList();

        var assigns = (await _assessmentRepo.GetAllAsync())
            .Where(a => a.TargetType == "Student")
            .Select(a => a.TargetValue)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        return new TestAssignData
        {
            TestId = test.Id,
            TestTitle = test.Title,
            Users = usersToShow.Cast<User>().ToList(),
            AssignedUserIds = assigns,
            Faculties = classes,
            SelectedFaculty = faculty
        };
    }

    public async Task<(bool Found, string Message)> AssignToUserAsync(string testId, string userId, DateTime? startAt = null, DateTime? endAt = null)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId);
        if (test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }

        if (!test.IsPublished)
        {
            test.IsPublished = true;
            test.PublishedAt = DateTime.UtcNow;
            await _testRepo.UpsertAsync(t => t.Id == testId, test);
        }

        var s = startAt ?? DateTime.UtcNow.AddDays(-1);
        var e = endAt ?? DateTime.UtcNow.AddDays(30);

        var assessment = new Assessment
        {
            Title = test.Title,
            StartTime = s,
            EndTime = e,
            TargetType = "Student",
            TargetValue = userId,
            CourseId = test.CourseId ?? "default",
            Type = AssessmentType.Quiz
        };
        await _assessmentRepo.InsertAsync(assessment);

        test.AssessmentId = assessment.Id;
        await _testRepo.UpsertAsync(t => t.Id == testId, test);

        var student = await _studentRepo.FirstOrDefaultAsync(x => x.Id == userId);
        if (student != null)
        {
            var targets = new[]
            {
                new AssignmentNotifyTarget { User = student, SessionId = string.Empty }
            };
            await NotifySafe(test, targets, s, e);
        }

        return (true, $"Đã assign test '{test.Title}' cho sinh viên '{userId}'.");
    }

    public async Task<(bool Found, string Message)> AssignUsersAsync(string testId, IReadOnlyCollection<string>? userIds, DateTime? startAt = null, DateTime? endAt = null)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId);
        if (test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }

        if (!test.IsPublished)
        {
            test.IsPublished = true;
            test.PublishedAt = DateTime.UtcNow;
            await _testRepo.UpsertAsync(t => t.Id == testId, test);
        }

        var s = startAt ?? DateTime.UtcNow.AddDays(-1);
        var e = endAt ?? DateTime.UtcNow.AddDays(30);

        if (!string.IsNullOrWhiteSpace(test.AssessmentId))
        {
            await _assessmentRepo.DeleteAsync(a => a.TargetType == "Student" && a.Id == test.AssessmentId);
        }

        var newAssigned = (userIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (newAssigned.Count == 0)
        {
            test.AssessmentId = "";
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            return (true, "Đã lưu: không có sinh viên nào được assign.");
        }

        foreach (var uid in newAssigned)
        {
            var assessment = new Assessment
            {
                Title = test.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = uid,
                CourseId = test.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(assessment);
            test.AssessmentId = assessment.Id;
        }
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);

        var allStudents = await _studentRepo.GetAllAsync();
        var targets = allStudents
            .Where(u => newAssigned.Contains(u.Id))
            .Select(u => new AssignmentNotifyTarget { User = (User)u, SessionId = string.Empty })
            .ToList();
        await NotifySafe(test, targets, s, e);

        return (true, "Đã lưu danh sách assign và publish test.");
    }

    public async Task<(bool Found, string Message)> AssignByFacultyAsync(string testId, string faculty, DateTime? startAt = null, DateTime? endAt = null)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId);
        if (test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }

        if (!test.IsPublished)
        {
            test.IsPublished = true;
            test.PublishedAt = DateTime.UtcNow;
            await _testRepo.UpsertAsync(t => t.Id == testId, test);
        }

        var s = startAt ?? DateTime.UtcNow.AddDays(-1);
        var e = endAt ?? DateTime.UtcNow.AddDays(30);

        var assessment = new Assessment
        {
            Title = test.Title,
            StartTime = s,
            EndTime = e,
            TargetType = "Class",
            TargetValue = faculty,
            CourseId = test.CourseId ?? "default",
            Type = AssessmentType.Quiz
        };
        await _assessmentRepo.InsertAsync(assessment);

        test.AssessmentId = assessment.Id;
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
        return (true, $"Đã gán bài thi cho các sinh viên của Lớp/Khoa '{faculty}'.");
    }

    public async Task<string> BulkAssignAsync(IReadOnlyCollection<string>? testIds, string userId, DateTime? startAt = null, DateTime? endAt = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Thiếu UserId.";
        }

        var ids = (testIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToList();
        if (!ids.Any())
        {
            return "Bạn chưa chọn Test nào.";
        }

        var s = startAt ?? DateTime.UtcNow.AddDays(-1);
        var e = endAt ?? DateTime.UtcNow.AddDays(30);

        var assigned = 0;
        foreach (var tid in ids)
        {
            var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == tid);
            if (test == null) continue;

            if (!test.IsPublished)
            {
                test.IsPublished = true;
                test.PublishedAt = DateTime.UtcNow;
                await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            }

            if (!string.IsNullOrWhiteSpace(test.AssessmentId))
            {
                await _assessmentRepo.DeleteAsync(a => a.Id == test.AssessmentId);
            }

            var assessment = new Assessment
            {
                Title = test.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = userId,
                CourseId = test.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(assessment);

            test.AssessmentId = assessment.Id;
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            assigned++;
        }

        return $"Đã assign {assigned} test cho sinh viên '{userId}'.";
    }

    public async Task<string> BulkAssignAutoAsync(IReadOnlyCollection<string>? testIds, DateTime? startAt = null, DateTime? endAt = null)
    {
        var ids = (testIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToList();
        if (!ids.Any())
        {
            return "Bạn chưa chọn Test nào.";
        }

        var students = await _studentRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();
        var testMap = tests.ToDictionary(t => t.Id, StringComparer.Ordinal);

        var s = startAt ?? DateTime.UtcNow.AddDays(-1);
        var e = endAt ?? DateTime.UtcNow.AddDays(30);

        var assigned = 0;
        var skipped = new List<string>();

        foreach (var tid in ids)
        {
            if (!testMap.TryGetValue(tid, out var test))
            {
                skipped.Add($"{tid} (not found)");
                continue;
            }

            var owner = ResolveOwnerUser(students.Cast<User>().ToList(), test);
            if (owner == null)
            {
                skipped.Add(test.Title);
                continue;
            }

            if (!test.IsPublished)
            {
                test.IsPublished = true;
                test.PublishedAt = DateTime.UtcNow;
                await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            }

            if (!string.IsNullOrWhiteSpace(test.AssessmentId))
            {
                await _assessmentRepo.DeleteAsync(a => a.Id == test.AssessmentId);
            }

            var assessment = new Assessment
            {
                Title = test.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = owner.Id,
                CourseId = test.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(assessment);

            test.AssessmentId = assessment.Id;
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            assigned++;
        }

        var msg = $"Đã assign {assigned} test (auto by owner).";
        if (skipped.Count > 0)
        {
            msg += $" Bỏ qua {skipped.Count}: {string.Join("; ", skipped.Take(5))}{(skipped.Count > 5 ? "..." : "")}";
        }
        return msg;
    }

    private async Task CreateSnapshotsAsync(Test test)
    {
        test.QuestionSnapshots ??= new List<TestQuestionSnapshot>();
        if (test.QuestionSnapshots.Any()) return;

        foreach (var tq in test.TestQuestions)
        {
            var question = await _questionService.GetAsync(tq.QuestionId);
            if (question == null) continue;

            var snapshot = new TestQuestionSnapshot
            {
                TestId = test.Id,
                OriginalQuestionId = question.Id,
                Content = question.Content,
                Type = question.Type,
                Points = tq.Points,
                Order = tq.Order,
                OptionsJson = System.Text.Json.JsonSerializer.Serialize(question.Options.Select(o => new { o.Content, o.IsCorrect })),
                CreatedAt = DateTime.UtcNow
            };
            test.QuestionSnapshots.Add(snapshot);
        }
    }

    private async Task NotifySafe(Test test, IEnumerable<AssignmentNotifyTarget> targets, DateTime startAtUtc, DateTime endAtUtc)
    {
        if (_notificationService == null) return;
        try
        {
            await _notificationService.NotifyAssignmentsAsync(test, targets, startAtUtc, endAtUtc);
        }
        catch
        {
            // ignore notification exceptions in assignment flow
        }
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static User? ResolveOwnerUser(List<User> users, Test test)
    {
        var title = test.Title ?? "";

        var mUid = Regex.Match(title, @"uid\s*:\s*(?<id>[A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
        if (mUid.Success)
        {
            var id = mUid.Groups["id"].Value.Trim();
            var byId = users.FirstOrDefault(u => string.Equals(u.Id, id, StringComparison.Ordinal));
            if (byId != null) return byId;
        }

        var mName = Regex.Match(title, @"^Auto\s*-\s*(?<name>[^-]+?)\s*-", RegexOptions.IgnoreCase);
        if (mName.Success)
        {
            var nameInTitle = mName.Groups["name"].Value.Trim();
            var normalizedTitleName = RemoveDiacritics(nameInTitle).ToLowerInvariant();

            var byName = users.FirstOrDefault(u =>
                RemoveDiacritics(u.Name ?? "").ToLowerInvariant() == normalizedTitleName);
            if (byName != null) return byName;

            var byEmailLocal = users.FirstOrDefault(u =>
            {
                var local = (u.Email ?? "").Split('@')[0];
                return RemoveDiacritics(local).ToLowerInvariant() == normalizedTitleName.Replace(" ", "");
            });
            if (byEmailLocal != null) return byEmailLocal;
        }

        return null;
    }
}
