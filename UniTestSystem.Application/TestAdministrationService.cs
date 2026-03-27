using System.Text;
using System.Text.RegularExpressions;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class TestAdministrationService : ITestAdministrationService
{
    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<Question> _questionRepo;
    private readonly IRepository<QuestionBank> _questionBankRepo;
    private readonly IRepository<Assessment> _assessmentRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<StudentClass> _studentClassRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IQuestionService _questionService;
    private readonly INotificationService? _notificationService;

    public TestAdministrationService(
        IRepository<Test> testRepo,
        IRepository<Question> questionRepo,
        IRepository<QuestionBank> questionBankRepo,
        IRepository<Assessment> assessmentRepo,
        IRepository<Student> studentRepo,
        IRepository<Enrollment> enrollmentRepo,
        IRepository<Course> courseRepo,
        IRepository<StudentClass> studentClassRepo,
        IRepository<User> userRepo,
        IQuestionService questionService,
        INotificationService? notificationService = null)
    {
        _testRepo = testRepo;
        _questionRepo = questionRepo;
        _questionBankRepo = questionBankRepo;
        _assessmentRepo = assessmentRepo;
        _studentRepo = studentRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _studentClassRepo = studentClassRepo;
        _userRepo = userRepo;
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
        }

        test.IsPublished = true;
        test.CreatedAt = DateTime.UtcNow;
        test.PublishedAt = DateTime.UtcNow;
        test.ShuffleQuestions = true;
        test.ShuffleOptions = true;

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
        test.AssessmentType = request.AssessmentType;
        test.CourseId = string.IsNullOrWhiteSpace(request.CourseId) ? null : request.CourseId.Trim();
        test.UpdatedAt = DateTime.UtcNow;

        var selectedIds = selectedQuestionIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList()
                          ?? new List<string>();
        if (selectedIds.Any())
        {
            test.TestQuestions = selectedIds
                .Select(qid => new TestQuestion { TestId = test.Id, QuestionId = qid })
                .ToList();
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
            AssessmentType = test.AssessmentType,
            ShuffleQuestions = test.ShuffleQuestions,
            ShuffleOptions = test.ShuffleOptions,
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

    public async Task<TestAssignData?> GetAssignDataAsync(string testId, string? classFilter, string? currentUserId = null, bool isPrivileged = false)
    {
        var (allowed, _, test, course, isOwner, isAdmin, _) =
            await ValidateAssignContextAsync(testId, currentUserId, isPrivileged);
        if (!allowed || test == null || course == null)
        {
            return null;
        }

        var enrolledStudentIds = await GetEnrolledStudentIdsAsync(course.Id);
        var allStudents = await _studentRepo.GetAllAsync(s => !s.IsDeleted);

        var scopedStudents = isAdmin
            ? allStudents
            : allStudents.Where(s => enrolledStudentIds.Contains(s.Id)).ToList();

        var usersToShow = string.IsNullOrWhiteSpace(classFilter)
            ? scopedStudents
            : scopedStudents.Where(s => string.Equals(s.StudentClassId, classFilter, StringComparison.Ordinal)).ToList();

        var assignedUserIds = (await _assessmentRepo.GetAllAsync(a =>
                !a.IsDeleted &&
                a.TargetType == "Student" &&
                a.CourseId == course.Id))
            .Select(a => a.TargetValue)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        var availableClasses = await ResolveAvailableClassesAsync(scopedStudents);
        var legacyClassIds = availableClasses
            .Select(c => c.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        return new TestAssignData
        {
            TestId = test.Id,
            TestTitle = test.Title,
            CourseName = course.Name,
            LecturerName = await ResolveLecturerNameAsync(course.LecturerId),
            TotalEnrolled = isAdmin ? allStudents.Count : enrolledStudentIds.Count,
            AvailableClasses = availableClasses,
            AvailableClassCodes = availableClasses.Select(c => c.Code).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList(),
            IsOwner = isOwner || isAdmin,
            Users = usersToShow.Cast<User>().ToList(),
            AssignedUserIds = assignedUserIds,
            Faculties = legacyClassIds,
            SelectedFaculty = classFilter,
            SelectedClassId = classFilter
        };
    }

    public async Task<(bool Found, string Message)> AssignToUserAsync(string testId, string userId, DateTime? startAt = null, DateTime? endAt = null)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId && !t.IsDeleted);
        if (test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }

        if (!TryGetAssessmentCourseId(test, out var courseId))
        {
            return (true, "Không thể giao bài: test chưa được gắn Course. Vui lòng vào Edit Test và chọn Course trước khi assign.");
        }

        if (startAt.HasValue && endAt.HasValue && startAt.Value >= endAt.Value)
        {
            return (true, "Không thể giao bài: thời gian bắt đầu phải trước thời gian kết thúc.");
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
            CourseId = courseId,
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

    public async Task<(bool Found, string Message)> AssignUsersAsync(
        string testId,
        IReadOnlyCollection<string>? userIds,
        DateTime? startAt = null,
        DateTime? endAt = null,
        string? currentUserId = null,
        bool isPrivileged = false)
    {
        var (allowed, found, test, course, _, isAdmin, denialMessage) =
            await ValidateAssignContextAsync(testId, currentUserId, isPrivileged);
        if (!found || test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }
        if (!allowed || course == null)
        {
            return (true, denialMessage);
        }

        if (startAt.HasValue && endAt.HasValue && startAt.Value >= endAt.Value)
        {
            return (true, "Không thể assign: thời gian bắt đầu phải trước thời gian kết thúc.");
        }

        var requestedUserIds = (userIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var enrolledStudentIds = await GetEnrolledStudentIdsAsync(course.Id);
        var validUserIds = isAdmin
            ? requestedUserIds
            : requestedUserIds.Where(id => enrolledStudentIds.Contains(id)).ToList();
        var skippedCount = requestedUserIds.Count - validUserIds.Count;

        foreach (var invalidUserId in requestedUserIds.Except(validUserIds, StringComparer.Ordinal))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[TestAdministrationService] Skip assigning test '{testId}' to user '{invalidUserId}' because user is not enrolled in course '{course.Id}'.");
        }

        if (!TryGetAssessmentCourseId(test, out var courseId))
        {
            return (true, "Không thể giao bài: test chưa được gắn Course. Vui lòng vào Edit Test và chọn Course trước khi assign.");
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

        if (validUserIds.Count == 0)
        {
            test.AssessmentId = "";
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            return skippedCount > 0
                ? (true, $"Đã assign 0 sinh viên. Bỏ qua {skippedCount} (không enrolled).")
                : (true, "Đã lưu: không có sinh viên nào được assign.");
        }

        foreach (var uid in validUserIds)
        {
            var assessment = new Assessment
            {
                Title = test.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = uid,
                CourseId = courseId,
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(assessment);
            test.AssessmentId = assessment.Id;
        }
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);

        var allStudents = await _studentRepo.GetAllAsync(su => !su.IsDeleted);
        var targets = allStudents
            .Where(u => validUserIds.Contains(u.Id))
            .Select(u => new AssignmentNotifyTarget { User = (User)u, SessionId = string.Empty })
            .ToList();
        await NotifySafe(test, targets, s, e);

        return skippedCount > 0
            ? (true, $"Đã assign {validUserIds.Count} sinh viên. Bỏ qua {skippedCount} (không enrolled).")
            : (true, $"Đã assign {validUserIds.Count} sinh viên.");
    }

    public Task<(bool Found, string Message)> AssignByFacultyAsync(
        string testId,
        string faculty,
        DateTime? startAt = null,
        DateTime? endAt = null,
        string? currentUserId = null,
        bool isPrivileged = false)
    {
        return AssignByClassAsync(testId, faculty, startAt, endAt, currentUserId, isPrivileged);
    }

    public async Task<(bool Found, string Message)> AssignByClassAsync(
        string testId,
        string classId,
        DateTime? startAt = null,
        DateTime? endAt = null,
        string? currentUserId = null,
        bool isPrivileged = false)
    {
        if (string.IsNullOrWhiteSpace(classId))
        {
            return (true, "Không thể assign theo lớp: thiếu ClassId.");
        }

        var (allowed, found, test, course, _, isAdmin, denialMessage) =
            await ValidateAssignContextAsync(testId, currentUserId, isPrivileged);
        if (!found || test == null)
        {
            return (false, "Không tìm thấy bài test.");
        }
        if (!allowed || course == null)
        {
            return (true, denialMessage);
        }

        if (startAt.HasValue && endAt.HasValue && startAt.Value >= endAt.Value)
        {
            return (true, "Không thể assign theo lớp: thời gian bắt đầu phải trước thời gian kết thúc.");
        }

        var studentClass = await _studentClassRepo.FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);
        if (studentClass == null)
        {
            return (true, "Không thể assign theo lớp: lớp không tồn tại.");
        }

        if (!TryGetAssessmentCourseId(test, out var courseId))
        {
            return (true, "Không thể giao bài: test chưa được gắn Course. Vui lòng vào Edit Test và chọn Course trước khi assign.");
        }

        var allStudents = await _studentRepo.GetAllAsync(s => !s.IsDeleted);
        var classStudents = allStudents
            .Where(s => string.Equals(s.StudentClassId, classId, StringComparison.Ordinal))
            .ToList();

        if (!isAdmin)
        {
            var enrolledStudentIds = await GetEnrolledStudentIdsAsync(courseId);
            classStudents = classStudents
                .Where(s => enrolledStudentIds.Contains(s.Id))
                .ToList();
        }

        if (classStudents.Count == 0)
        {
            return (true, $"Không thể assign theo lớp: không có sinh viên hợp lệ trong lớp '{studentClass.Code}'.");
        }

        if (!test.IsPublished)
        {
            test.IsPublished = true;
            test.PublishedAt = DateTime.UtcNow;
            await _testRepo.UpsertAsync(t => t.Id == testId, test);
        }

        var sAt = startAt ?? DateTime.UtcNow.AddDays(-1);
        var eAt = endAt ?? DateTime.UtcNow.AddDays(30);

        var classAssessment = new Assessment
        {
            Title = test.Title,
            StartTime = sAt,
            EndTime = eAt,
            TargetType = "Class",
            TargetValue = classId,
            CourseId = courseId,
            Type = AssessmentType.Quiz
        };
        await _assessmentRepo.InsertAsync(classAssessment);

        foreach (var student in classStudents)
        {
            var studentAssessment = new Assessment
            {
                Title = test.Title,
                StartTime = sAt,
                EndTime = eAt,
                TargetType = "Student",
                TargetValue = student.Id,
                CourseId = courseId,
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(studentAssessment);
        }

        test.AssessmentId = classAssessment.Id;
        await _testRepo.UpsertAsync(x => x.Id == test.Id, test);

        var notifyTargets = classStudents
            .Select(student => new AssignmentNotifyTarget { User = student, SessionId = string.Empty })
            .ToList();
        await NotifySafe(test, notifyTargets, sAt, eAt);

        return (true, $"Đã assign {classStudents.Count} sinh viên lớp '{studentClass.Code}'.");
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
        var skippedNoCourse = new List<string>();
        foreach (var tid in ids)
        {
            var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == tid);
            if (test == null) continue;

            if (!TryGetAssessmentCourseId(test, out var courseId))
            {
                skippedNoCourse.Add(test.Title);
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
                TargetValue = userId,
                CourseId = courseId,
                Type = AssessmentType.Quiz
            };
            await _assessmentRepo.InsertAsync(assessment);

            test.AssessmentId = assessment.Id;
            await _testRepo.UpsertAsync(x => x.Id == test.Id, test);
            assigned++;
        }

        var msg = $"Đã assign {assigned} test cho sinh viên '{userId}'.";
        if (skippedNoCourse.Count > 0)
        {
            msg += $" Bỏ qua {skippedNoCourse.Count} test chưa gắn Course: {string.Join("; ", skippedNoCourse.Take(3))}{(skippedNoCourse.Count > 3 ? "..." : "")}";
        }
        return msg;
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
        var skippedNoCourse = new List<string>();

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

            if (!TryGetAssessmentCourseId(test, out var courseId))
            {
                skippedNoCourse.Add(test.Title);
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
                CourseId = courseId,
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
        if (skippedNoCourse.Count > 0)
        {
            msg += $" Bỏ qua {skippedNoCourse.Count} test chưa gắn Course: {string.Join("; ", skippedNoCourse.Take(3))}{(skippedNoCourse.Count > 3 ? "..." : "")}";
        }
        return msg;
    }

    public async Task<List<Question>> PreviewQuestionsAsync(PreviewQuestionsRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CourseId))
        {
            return new List<Question>();
        }

        var courseId = request.CourseId.Trim();
        var bankIds = (await _questionBankRepo.GetAllAsync(x =>
                !x.IsDeleted &&
                x.CourseId == courseId))
            .Select(x => x.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (bankIds.Count == 0)
        {
            return new List<Question>();
        }

        var approvedQuestions = await _questionRepo.GetAllAsync(x =>
            !x.IsDeleted &&
            x.IsActive &&
            x.Status == QuestionStatus.Approved);

        var coursePool = approvedQuestions
            .Where(x => !string.IsNullOrWhiteSpace(x.QuestionBankId) && bankIds.Contains(x.QuestionBankId!))
            .ToList();

        var picked = new List<Question>();
        picked.AddRange(PickRandomByType(coursePool, QType.MCQ, request.McqCount));
        picked.AddRange(PickRandomByType(coursePool, QType.TrueFalse, request.TfCount));
        picked.AddRange(PickRandomByType(coursePool, QType.Essay, request.EssayCount));
        picked.AddRange(PickRandomByType(coursePool, QType.Matching, request.MatchingCount));
        picked.AddRange(PickRandomByType(coursePool, QType.DragDrop, request.DragDropCount));

        return Shuffle(picked);
    }

    private async Task<(bool Allowed, bool Found, Test? Test, Course? Course, bool IsOwner, bool IsAdmin, string Message)> ValidateAssignContextAsync(
        string testId,
        string? currentUserId,
        bool isPrivileged)
    {
        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId && !t.IsDeleted);
        if (test == null)
        {
            return (false, false, null, null, false, false, "Không tìm thấy bài test.");
        }

        if (!TryGetAssessmentCourseId(test, out var courseId))
        {
            return (false, true, test, null, false, false, "Không thể assign: test chưa được gắn Course.");
        }

        var course = await _courseRepo.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
        if (course == null)
        {
            return (false, true, test, null, false, false, "Không thể assign: Course không tồn tại hoặc đã bị xóa.");
        }

        var isOwner = !string.IsNullOrWhiteSpace(currentUserId) &&
                      string.Equals(course.LecturerId, currentUserId, StringComparison.Ordinal);

        var isAdmin = false;
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var caller = await _userRepo.FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted);
            isAdmin = caller?.Role == Role.Admin;
        }

        if (!isPrivileged && !isOwner)
        {
            return (false, true, test, course, false, isAdmin, "Không thể assign: bạn không có quyền trên Course này.");
        }

        return (true, true, test, course, isOwner, isAdmin, string.Empty);
    }

    private static List<Question> PickRandomByType(IReadOnlyCollection<Question> pool, QType type, int count)
    {
        var safeCount = Math.Max(0, count);
        if (safeCount == 0 || pool.Count == 0)
        {
            return new List<Question>();
        }

        return Shuffle(pool.Where(x => x.Type == type).ToList())
            .Take(safeCount)
            .ToList();
    }

    private static List<Question> Shuffle(List<Question> items)
    {
        if (items.Count <= 1)
        {
            return items;
        }

        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }

    private async Task<HashSet<string>> GetEnrolledStudentIdsAsync(string courseId)
    {
        return (await _enrollmentRepo.GetAllAsync(e => e.CourseId == courseId && !e.IsDeleted))
            .Select(e => e.StudentId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<StudentClass>> ResolveAvailableClassesAsync(IEnumerable<Student> students)
    {
        var classIds = students
            .Select(s => s.StudentClassId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (classIds.Count == 0)
        {
            return new List<StudentClass>();
        }

        var classes = await _studentClassRepo.GetAllAsync(c => !c.IsDeleted);
        var matched = classes
            .Where(c => classIds.Contains(c.Id))
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var knownIds = matched.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var missingIds = classIds.Where(id => !knownIds.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
        foreach (var missingId in missingIds)
        {
            matched.Add(new StudentClass
            {
                Id = missingId,
                Code = missingId,
                Name = missingId
            });
        }

        return matched;
    }

    private async Task<string> ResolveLecturerNameAsync(string lecturerId)
    {
        if (string.IsNullOrWhiteSpace(lecturerId))
        {
            return "Chưa gán";
        }

        var lecturer = await _userRepo.FirstOrDefaultAsync(u => u.Id == lecturerId && !u.IsDeleted);
        return !string.IsNullOrWhiteSpace(lecturer?.Name) ? lecturer.Name : lecturerId;
    }

    private static bool TryGetAssessmentCourseId(Test test, out string courseId)
    {
        courseId = (test.CourseId ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(courseId);
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
