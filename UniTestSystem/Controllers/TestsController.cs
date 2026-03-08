using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Tests_View)]
    public class TestsController : Controller
    {
        private readonly IRepository<Test> _repo;
        private readonly IRepository<Assessment> _asRepo;
        private readonly IRepository<Student> _sRepo;
        private readonly IQuestionService _questionService;
        private readonly INotificationService? _notiService;

        public TestsController(
            IRepository<Test> r,
            IRepository<Assessment> asRepo,
            IRepository<Student> sRepo,
            IQuestionService questionService,
            INotificationService? notiService = null)
        {
            _repo = r;
            _asRepo = asRepo;
            _sRepo = sRepo;
            _questionService = questionService;
            _notiService = notiService;
        }

        // ---------- Index ----------
        public async Task<IActionResult> Index(string? status = null)
        {
            var list = await _repo.GetAllAsync();
            
            // Only show non-archived by default
            if (status != "Archived")
            {
                list = list.Where(t => !t.IsArchived).ToList();
            }
            else
            {
                list = list.Where(t => t.IsArchived).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Archived")
            {
                if (string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
                    list = list.Where(t => !t.IsPublished).ToList();
                else if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
                    list = list.Where(t => t.IsPublished).ToList();
            }

            return View(list);
        }

        // ---------- Create (GET) ----------
        [HttpGet]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Create([FromQuery] QuestionFilter f)
        {
            if (f.Page <= 0) f.Page = 1;

            // Hỗ trợ ?PageSize=all
            var psRawQS = HttpContext.Request.Query["PageSize"].ToString();
            if (string.Equals(psRawQS, "all", StringComparison.OrdinalIgnoreCase))
                f.PageSize = int.MaxValue;
            else if (f.PageSize <= 0)
                f.PageSize = 20;

            var paged = await _questionService.SearchAsync(f);
            var model = new CreateTestViewModel
            {
                Filter = f,
                Page = paged,
                Title = "",
                DurationMinutes = 10,
                PassScore = 3,
                SubjectIdFilter = "Programming",
                RandomMCQ = 2,
                RandomTF = 1,
                RandomEssay = 0
            };

            // NEW: giữ giá trị form khi paging (đọc từ query)
            string q(string key) => HttpContext.Request.Query[key].ToString();
            if (!string.IsNullOrWhiteSpace(q("Title"))) model.Title = q("Title");
            if (int.TryParse(q("DurationMinutes"), out var dur)) model.DurationMinutes = dur;
            if (int.TryParse(q("PassScore"), out var pass)) model.PassScore = pass;
            if (!string.IsNullOrWhiteSpace(q("SubjectIdFilter"))) model.SubjectIdFilter = q("SubjectIdFilter");
            if (int.TryParse(q("RandomMCQ"), out var r1)) model.RandomMCQ = r1;
            if (int.TryParse(q("RandomTF"), out var r2)) model.RandomTF = r2;
            if (int.TryParse(q("RandomEssay"), out var r3)) model.RandomEssay = r3;

            return View(model);
        }


        // ---------- Create (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Create(Test t, [FromForm] List<string>? SelectedQuestionIds)
        {
            if (SelectedQuestionIds != null && SelectedQuestionIds.Any())
            {
                t.TestQuestions = SelectedQuestionIds.Distinct().Select(qid => new TestQuestion { QuestionId = qid }).ToList();
                t.RandomMCQ = 0; t.RandomTF = 0; t.RandomEssay = 0;
            }
            else
            {
                if (t.RandomMCQ + t.RandomTF + t.RandomEssay <= 0)
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 câu hỏi hoặc cấu hình số lượng random > 0");
            }

            if (!ModelState.IsValid)
            {
                var f = new QuestionFilter
                {
                    Page = int.TryParse(HttpContext.Request.Query["Page"], out var p) ? p : 1,
                    PageSize = int.TryParse(HttpContext.Request.Query["PageSize"], out var ps) ? ps : 20,
                    Keyword = HttpContext.Request.Query["Keyword"],
                    SubjectId = HttpContext.Request.Query["Subject"],
                    DifficultyLevelId = HttpContext.Request.Query["DifficultyLevel"],
                    TagsCsv = HttpContext.Request.Query["TagsCsv"],
                    Sort = HttpContext.Request.Query["Sort"]
                };
                var psRaw = HttpContext.Request.Query["PageSize"].ToString();
                if (string.Equals(psRaw, "all", StringComparison.OrdinalIgnoreCase))
                    f.PageSize = int.MaxValue;

                var paged = await _questionService.SearchAsync(f);

                var vm = new CreateTestViewModel
                {
                    Filter = f,
                    Page = paged,
                    Title = t.Title,
                    DurationMinutes = t.DurationMinutes,
                    PassScore = t.PassScore,
                    SubjectIdFilter = t.SubjectIdFilter,
                    RandomMCQ = t.RandomMCQ,
                    RandomTF = t.RandomTF,
                    RandomEssay = t.RandomEssay,
                    SelectedQuestionIds = SelectedQuestionIds ?? new List<string>()
                };
                return View(vm);
            }

            t.IsPublished = true;
            t.CreatedAt = DateTime.UtcNow;
            t.PublishedAt = DateTime.UtcNow;
            t.ShuffleQuestions = true; // Default
            t.ShuffleOptions = true;   // Default
            t.FrozenRandom = new FrozenRandomConfig
            {
                SubjectIdFilter = t.SubjectIdFilter,
                RandomMCQ = t.RandomMCQ,
                RandomTF = t.RandomTF,
                RandomEssay = t.RandomEssay
            };

            await _repo.InsertAsync(t);
            
            // Create snapshots for non-random questions
            if (t.TestQuestions.Any())
            {
                await CreateSnapshotsAsync(t);
                await _repo.UpsertAsync(x => x.Id == t.Id, t);
            }

            TempData["Msg"] = "Đã tạo và publish bài test.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit (GET) ----------
        [HttpGet]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Edit(string id, [FromQuery] QuestionFilter f)
        {
            var t = await _repo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            if (f.Page <= 0) f.Page = 1;

            // Hỗ trợ ?PageSize=all
            var psRawQS = HttpContext.Request.Query["PageSize"].ToString();
            if (string.Equals(psRawQS, "all", StringComparison.OrdinalIgnoreCase))
                f.PageSize = int.MaxValue;
            else if (f.PageSize <= 0)
                f.PageSize = 20;

            var paged = await _questionService.SearchAsync(f);

            var vm = new EditTestViewModel
            {
                Id = t.Id,
                Title = t.Title,
                DurationMinutes = t.DurationMinutes,
                PassScore = t.PassScore,
                ShuffleQuestions = t.ShuffleQuestions,
                SubjectIdFilter = t.SubjectIdFilter,
                RandomMCQ = t.RandomMCQ,
                RandomTF = t.RandomTF,
                RandomEssay = t.RandomEssay,
                IsPublished = t.IsPublished,
                Filter = f,
                Page = paged,
                SelectedQuestionIds = t.TestQuestions.Select(tq => tq.QuestionId).ToList()
            };

            // NEW: giữ giá trị form khi paging
            string q(string key) => HttpContext.Request.Query[key].ToString();
            if (!string.IsNullOrWhiteSpace(q("Title"))) vm.Title = q("Title");
            if (int.TryParse(q("DurationMinutes"), out var dur)) vm.DurationMinutes = dur;
            if (int.TryParse(q("PassScore"), out var pass)) vm.PassScore = pass;
            if (bool.TryParse(q("ShuffleQuestions"), out var sh)) vm.ShuffleQuestions = sh;
            if (!string.IsNullOrWhiteSpace(q("SubjectIdFilter"))) vm.SubjectIdFilter = q("SubjectIdFilter");
            if (int.TryParse(q("RandomMCQ"), out var r1)) vm.RandomMCQ = r1;
            if (int.TryParse(q("RandomTF"), out var r2)) vm.RandomTF = r2;
            if (int.TryParse(q("RandomEssay"), out var r3)) vm.RandomEssay = r3;

            return View(vm);
        }

        // ---------- Edit (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Edit(EditTestViewModel vm, [FromForm] List<string>? SelectedQuestionIds)
        {
            var t = await _repo.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (t == null) return NotFound();

            if ((SelectedQuestionIds == null || !SelectedQuestionIds.Any()) &&
                (vm.RandomMCQ + vm.RandomTF + vm.RandomEssay <= 0))
            {
                ModelState.AddModelError("", "Chọn ít nhất 1 câu hỏi hoặc cấu hình random > 0.");
            }

            if (!ModelState.IsValid)
            {
                var f = new QuestionFilter
                {
                    Page = int.TryParse(HttpContext.Request.Query["Page"], out var p) ? p : 1,
                    PageSize = int.TryParse(HttpContext.Request.Query["PageSize"], out var ps) ? ps : 20,
                    Keyword = HttpContext.Request.Query["Keyword"],
                    SubjectId = HttpContext.Request.Query["Subject"],
                    DifficultyLevelId = HttpContext.Request.Query["DifficultyLevel"],
                    TagsCsv = HttpContext.Request.Query["TagsCsv"],
                    Sort = HttpContext.Request.Query["Sort"]
                };
                var psRaw = HttpContext.Request.Query["PageSize"].ToString();
                if (string.Equals(psRaw, "all", StringComparison.OrdinalIgnoreCase))
                    f.PageSize = int.MaxValue;

                vm.Filter = f;
                vm.Page = await _questionService.SearchAsync(f);
                vm.SelectedQuestionIds = SelectedQuestionIds ?? new List<string>();
                return View(vm);
            }

            t.Title = vm.Title;
            t.DurationMinutes = vm.DurationMinutes;
            t.PassScore = vm.PassScore;
            t.ShuffleQuestions = vm.ShuffleQuestions;
            t.ShuffleOptions = vm.ShuffleOptions;
            t.SubjectIdFilter = vm.SubjectIdFilter;
            t.RandomMCQ = vm.RandomMCQ;
            t.RandomTF = vm.RandomTF;
            t.RandomEssay = vm.RandomEssay;
            t.UpdatedAt = DateTime.UtcNow;

            if (SelectedQuestionIds != null && SelectedQuestionIds.Any())
            {
                t.TestQuestions = SelectedQuestionIds.Distinct().Select(qid => new TestQuestion { TestId = t.Id, QuestionId = qid }).ToList();
                t.RandomMCQ = 0; t.RandomTF = 0; t.RandomEssay = 0;
            }
            else
            {
                t.TestQuestions = new List<TestQuestion>();
            }

            await _repo.UpsertAsync(x => x.Id == t.Id, t);
            TempData["Msg"] = "Đã lưu thay đổi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Delete ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Delete(string id)
        {
            var test = await _repo.FirstOrDefaultAsync(t => t.Id == id);
            if (test?.AssessmentId != null)
            {
                await _asRepo.DeleteAsync(a => a.Id == test.AssessmentId);
            }
            await _repo.DeleteAsync(t => t.Id == id);
            TempData["Msg"] = "Đã xoá bài test.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Toggle publish ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Publish)]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var t = await _repo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            t.IsPublished = !t.IsPublished;
            if (t.IsPublished)
            {
                t.PublishedAt = DateTime.UtcNow;
                t.FrozenRandom = new FrozenRandomConfig
                {
                    SubjectIdFilter = t.SubjectIdFilter,
                    RandomMCQ = t.RandomMCQ,
                    RandomTF = t.RandomTF,
                    RandomEssay = t.RandomEssay
                };

                // Create snapshots on publish
                if (t.TestQuestions.Any())
                {
                    await CreateSnapshotsAsync(t);
                }

                TempData["Msg"] = "Đã chuyển sang Published và tạo Snapshot câu hỏi.";
            }
            else
            {
                t.PublishedAt = null;
                TempData["Msg"] = "Đã chuyển sang Draft.";
            }

            await _repo.UpsertAsync(x => x.Id == t.Id, t);
            return RedirectToAction(nameof(Index));
        }

        // ---------- Clone ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Clone(string id)
        {
            var t = await _repo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            var clone = new Test
            {
                Title = t.Title + " (Clone)",
                DurationMinutes = t.DurationMinutes,
                PassScore = t.PassScore,
                ShuffleQuestions = t.ShuffleQuestions,
                ShuffleOptions = t.ShuffleOptions,
                SubjectIdFilter = t.SubjectIdFilter,
                RandomMCQ = t.RandomMCQ,
                RandomTF = t.RandomTF,
                RandomEssay = t.RandomEssay,
                TotalMaxScore = t.TotalMaxScore,
                CourseId = t.CourseId,
                IsPublished = false,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                TestQuestions = t.TestQuestions.Select(q => new TestQuestion { QuestionId = q.QuestionId, Points = q.Points }).ToList()
            };

            await _repo.InsertAsync(clone);
            TempData["Msg"] = "Đã nhân bản đề thi (Draft).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Archive ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Archive(string id)
        {
            var t = await _repo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();

            t.IsArchived = true;
            await _repo.UpsertAsync(x => x.Id == t.Id, t);
            TempData["Msg"] = "Đã chuyển đề thi vào mục Lưu trữ (Archive).";
            return RedirectToAction(nameof(Index));
        }

        private async Task CreateSnapshotsAsync(Test test)
        {
            // Nếu đã có snapshot thì không cần tạo lại (tránh tốn tài nguyên)
            if (test.QuestionSnapshots != null && test.QuestionSnapshots.Any()) return;

            foreach (var tq in test.TestQuestions)
            {
                var q = await _questionService.GetAsync(tq.QuestionId);
                if (q == null) continue;

                var snapshot = new TestQuestionSnapshot
                {
                    TestId = test.Id,
                    OriginalQuestionId = q.Id,
                    Content = q.Content,
                    Type = q.Type,
                    Points = tq.Points,
                    Order = tq.Order,
                    OptionsJson = System.Text.Json.JsonSerializer.Serialize(q.Options.Select(o => new { o.Content, o.IsCorrect })),
                    CreatedAt = DateTime.UtcNow
                };
                test.QuestionSnapshots.Add(snapshot);
            }
        }

        // ---------- Assign nhanh (1 user) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> AssignToUser(string testId, string userId)
        {
            var test = await _repo.FirstOrDefaultAsync(t => t.Id == testId);
            if (test != null && !test.IsPublished)
            {
                test.IsPublished = true;
                test.PublishedAt = DateTime.UtcNow;
                await _repo.UpsertAsync(t => t.Id == testId, test);
            }

            var s = DateTime.UtcNow.AddDays(-1);
            var e = DateTime.UtcNow.AddDays(30);

            var assessment = new Assessment
            {
                Title = test?.Title ?? "Quick Quiz",
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = userId,
                CourseId = test?.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _asRepo.InsertAsync(assessment);
            if (test != null)
            {
                test.AssessmentId = assessment.Id;
                await _repo.UpsertAsync(t => t.Id == testId, test);
            }

            var u = await _sRepo.FirstOrDefaultAsync(x => x.Id == userId);
            if (test != null && u != null)
            {
                var targets = new[]
                {
                    new AssignmentNotifyTarget { User = u, SessionId = string.Empty }
                };
                await NotifySafe(test, targets, s, e);
            }

            TempData["Msg"] = $"Đã assign test '{test?.Title ?? testId}' cho sinh viên '{userId}'" +
                               (_notiService != null ? " và đã gửi thông báo/email." : ".");
            return RedirectToAction(nameof(Index));
        }

        // ---------- Assign (GET) ----------
        [HttpGet("Tests/Assign/{id}")]
        [HttpGet("/Tests/Assign")]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> Assign(string id, [FromQuery] string? faculty = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var test = await _repo.FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            var allStudents = await _sRepo.GetAllAsync();

            var classes = allStudents
                .Select(u => u.StudentClassId ?? "")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            var usersToShow = allStudents;
            if (!string.IsNullOrWhiteSpace(faculty))
            {
                usersToShow = allStudents
                    .Where(u => string.Equals(u.StudentClassId ?? "", faculty, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var assigns = (await _asRepo.GetAllAsync())
                .Where(a => a.TargetType == "Student")
                .Select(a => a.TargetValue)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.Ordinal);

            var vm = new AssignUsersViewModel
            {
                TestId = id,
                TestTitle = test.Title,
                Users = usersToShow.Cast<User>().ToList(),
                AssignedUserIds = assigns.Cast<string>().ToHashSet(),
                Faculties = classes,
                SelectedFaculty = faculty
            };
            return View(vm);
        }

        // ---------- Assign (POST) ----------
        [HttpPost("/Tests/Assign")]
        [HttpPost("Tests/Assign/{id?}")] // chấp nhận cả dạng có {id} để tránh 405
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> Assign(
            [FromRoute] string? id,                 // không dùng, chỉ để match route nếu có
            [FromForm] string testId,
            [FromForm] List<string>? userIds,
            [FromForm] DateTime? startAt,
            [FromForm] DateTime? endAt)
        {
            var test = await _repo.FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null) return NotFound();

            if (!test.IsPublished)
            {
                test.IsPublished = true;
                test.PublishedAt = DateTime.UtcNow;
                await _repo.UpsertAsync(t => t.Id == testId, test);
            }

            var s = startAt ?? DateTime.UtcNow.AddDays(-1);
            var e = endAt ?? DateTime.UtcNow.AddDays(30);

            // clear cũ: Một test link tới một Assessment. Ở đây nếu assign nhiều user, tui sẽ tạo nhiều Assessment?
            // Hay một Assessment cho nhiều user? Hiện tại Assessment chưa có bảng bridge cho Student.
            // Để giữ logic cũ, tui sẽ tạo mỗi user 1 Assessment record (giống Assignment cũ).
            // Nếu muốn tối ưu, cần bảng AssessmentTarget. Nhưng theo yêu cầu "Assessment replaces Assignment", tui làm tiếp như cũ.
            
            await _asRepo.DeleteAsync(a => a.TargetType == "Student" && 
                                            test.AssessmentId != null && a.Id == test.AssessmentId);

            var newAssigned = (userIds ?? new List<string>()).Distinct().ToList();

            if (newAssigned.Count == 0)
            {
                test.AssessmentId = "";
                await _repo.UpsertAsync(x => x.Id == test.Id, test);
                TempData["Msg"] = "Đã lưu: không có sinh viên nào được assign.";
                return RedirectToAction(nameof(Assign), new { id = testId });
            }

            // Để đơn giản, tui lấy user đầu tiên hoặc tạo Assessment tổng quát.
            // Nhưng thiết kế cũ là 1 test -> 1 assignment (cho 1 target).
            // Nếu chọn nhiều user, tui sẽ tạo nhiều Assessment? Không, tui sẽ tạo 1 Assessment với TargetType="Student" và TargetValue=string.Join(",", userIds).
            // HOẶC tui tạo nhiều Assessment. Tui sẽ tạo nhiều để giống logic cũ.
            
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
                await _asRepo.InsertAsync(assessment);
                
                // Cập nhật AssessmentId cuối cùng cho Test (đây là hạn chế của 1-1 ở đây)
                test.AssessmentId = assessment.Id;
            }
            await _repo.UpsertAsync(x => x.Id == test.Id, test);

            // notify user mới
            var allStudents = await _sRepo.GetAllAsync();
            var targets = allStudents
                .Where(u => newAssigned.Contains(u.Id))
                .Select(u => new AssignmentNotifyTarget { User = (User)u, SessionId = string.Empty })
                .ToList();

            if (targets.Count > 0)
                await NotifySafe(test!, targets, s, e);

            TempData["Msg"] = "Đã lưu danh sách assign và publish test" +
                               (_notiService != null ? " (đã gửi thông báo/email cho sinh viên)." : ".");
            return RedirectToAction(nameof(Assign), new { id = testId });
        }

        // ---------- AssignByFaculty ----------
        [HttpPost("/Tests/AssignByFaculty")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> AssignByFaculty(string testId, string faculty, DateTime? startAt, DateTime? endAt)
        {
            var test = await _repo.FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null) return NotFound();

            if (string.IsNullOrWhiteSpace(faculty))
            {
                TempData["Err"] = "Vui lòng chọn Lớp/Khoa.";
                return RedirectToAction(nameof(Assign), new { id = testId });
            }

            if (!test.IsPublished)
            {
                test.IsPublished = true;
                test.PublishedAt = DateTime.UtcNow;
                await _repo.UpsertAsync(t => t.Id == testId, test);
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
            await _asRepo.InsertAsync(assessment);
            
            test.AssessmentId = assessment.Id;
            await _repo.UpsertAsync(x => x.Id == test.Id, test);

            TempData["Msg"] = $"Đã gán bài thi cho các sinh viên của Lớp/Khoa '{faculty}'.";
            return RedirectToAction(nameof(Assign), new { id = testId, faculty });
        }

        // ---------- BulkAssign ----------
        [HttpPost("/Tests/BulkAssign")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> BulkAssign([FromForm] List<string> testIds, string userId, DateTime? startAt, DateTime? endAt)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Err"] = "Thiếu UserId.";
                return RedirectToAction(nameof(Index));
            }
            if (testIds == null || testIds.Count == 0)
            {
                TempData["Err"] = "Bạn chưa chọn Test nào.";
                return RedirectToAction(nameof(Index));
            }

            var s = startAt ?? DateTime.UtcNow.AddDays(-1);
            var e = endAt ?? DateTime.UtcNow.AddDays(30);

            int assigned = 0;
            foreach (var tid in testIds.Distinct())
            {
                var t = await _repo.FirstOrDefaultAsync(x => x.Id == tid);
                if (t == null) continue;

                if (!t.IsPublished)
                {
                    t.IsPublished = true;
                    t.PublishedAt = DateTime.UtcNow;
                    await _repo.UpsertAsync(x => x.Id == t.Id, t);
                }

                if (t.AssessmentId != null)
                {
                    await _asRepo.DeleteAsync(a => a.Id == t.AssessmentId);
                }

                var assessment = new Assessment
                {
                    Title = t.Title,
                    StartTime = s,
                    EndTime = e,
                    TargetType = "Student",
                    TargetValue = userId,
                    CourseId = t.CourseId ?? "default",
                    Type = AssessmentType.Quiz
                };
                await _asRepo.InsertAsync(assessment);
                
                t.AssessmentId = assessment.Id;
                await _repo.UpsertAsync(x => x.Id == t.Id, t);
                assigned++;
            }

            TempData["Msg"] = $"Đã assign {assigned} test cho sinh viên '{userId}'.";
            return RedirectToAction(nameof(Index), new { status = "Draft" });
        }

        // ---------- BulkAssignAuto ----------
        [HttpPost("/Tests/BulkAssignAuto")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> BulkAssignAuto([FromForm] List<string> testIds, DateTime? startAt, DateTime? endAt)
        {
            if (testIds == null || testIds.Count == 0)
            {
                TempData["Err"] = "Bạn chưa chọn Test nào.";
                return RedirectToAction(nameof(Index));
            }

            var students = await _sRepo.GetAllAsync();
            var tests = await _repo.GetAllAsync();

            var testMap = tests.ToDictionary(t => t.Id, t => t);
            var s = startAt ?? DateTime.UtcNow.AddDays(-1);
            var e = endAt ?? DateTime.UtcNow.AddDays(30);

            int assigned = 0;
            var skipped = new List<string>();

            foreach (var tid in testIds.Distinct())
            {
                if (!testMap.TryGetValue(tid, out var t)) { skipped.Add($"{tid} (not found)"); continue; }

                var owner = ResolveOwnerUser(students.Cast<User>().ToList(), t);
                if (owner == null) { skipped.Add(t.Title); continue; }

                if (!t.IsPublished)
                {
                    t.IsPublished = true;
                    t.PublishedAt = DateTime.UtcNow;
                    await _repo.UpsertAsync(x => x.Id == t.Id, t);
                }

                if (t.AssessmentId != null)
                {
                    await _asRepo.DeleteAsync(a => a.Id == t.AssessmentId);
                }

                var assessment = new Assessment
                {
                    Title = t.Title,
                    StartTime = s,
                    EndTime = e,
                    TargetType = "Student",
                    TargetValue = owner.Id,
                    CourseId = t.CourseId ?? "default",
                    Type = AssessmentType.Quiz
                };
                await _asRepo.InsertAsync(assessment);
                
                t.AssessmentId = assessment.Id;
                await _repo.UpsertAsync(x => x.Id == t.Id, t);
                assigned++;
            }

            var msg = $"Đã assign {assigned} test (auto by owner).";
            if (skipped.Count > 0)
                msg += $" Bỏ qua {skipped.Count}: {string.Join("; ", skipped.Take(5))}{(skipped.Count > 5 ? "..." : "")}";
            TempData["Msg"] = msg;

            return RedirectToAction(nameof(Index), new { status = "Draft" });
        }

        // ---------- Helpers ----------
        private async Task NotifySafe(
            Test test,
            IEnumerable<AssignmentNotifyTarget> targets,
            DateTime startAtUtc,
            DateTime endAtUtc)
        {
            if (_notiService == null) return;
            try
            {
                await _notiService.NotifyAssignmentsAsync(test, targets, startAtUtc, endAtUtc);
            }
            catch { /* log nếu có */ }
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private User? ResolveOwnerUser(List<User> users, Test t)
        {
            var title = t.Title ?? "";

            // 1) uid: <id>
            var mUid = Regex.Match(title, @"uid\s*:\s*(?<id>[A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (mUid.Success)
            {
                var id = mUid.Groups["id"].Value.Trim();
                var byId = users.FirstOrDefault(u => string.Equals(u.Id, id, StringComparison.Ordinal));
                if (byId != null) return byId;
            }

            // 2) Auto - <FullName> - ...
            var mName = Regex.Match(title, @"^Auto\s*-\s*(?<name>[^-]+?)\s*-", RegexOptions.IgnoreCase);
            if (mName.Success)
            {
                var nameInTitle = mName.Groups["name"].Value.Trim();
                var normTitleName = RemoveDiacritics(nameInTitle).ToLowerInvariant();

                var byName = users.FirstOrDefault(u =>
                    RemoveDiacritics(u.Name ?? "").ToLowerInvariant() == normTitleName);
                if (byName != null) return byName;

                var byEmailLocal = users.FirstOrDefault(u =>
                {
                    var local = (u.Email ?? "").Split('@')[0];
                    return RemoveDiacritics(local).ToLowerInvariant() == normTitleName.Replace(" ", "");
                });
                if (byEmailLocal != null) return byEmailLocal;
            }

            return null;
        }
    }
}
