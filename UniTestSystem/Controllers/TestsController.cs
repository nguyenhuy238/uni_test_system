using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniTestSystem.ViewModels.Tests;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Tests_View)]
    public class TestsController : Controller
    {
        private readonly ITestAdministrationService _testAdministrationService;
        private readonly IQuestionService _questionService;

        public TestsController(
            ITestAdministrationService testAdministrationService,
            IQuestionService questionService)
        {
            _testAdministrationService = testAdministrationService;
            _questionService = questionService;
        }

        // ---------- Index ----------
        public async Task<IActionResult> Index(string? status = null)
        {
            var list = await _testAdministrationService.GetTestsByStatusAsync(status);
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

            await _testAdministrationService.CreateAndPublishAsync(t, SelectedQuestionIds);

            TempData["Msg"] = "Đã tạo và publish bài test.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit (GET) ----------
        [HttpGet]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Edit(string id, [FromQuery] QuestionFilter f)
        {
            var t = await _testAdministrationService.GetTestByIdAsync(id);
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
            var existing = await _testAdministrationService.GetTestByIdAsync(vm.Id);
            if (existing == null) return NotFound();

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

            var request = new TestUpdateRequest
            {
                Id = vm.Id,
                Title = vm.Title,
                DurationMinutes = vm.DurationMinutes,
                PassScore = vm.PassScore,
                ShuffleQuestions = vm.ShuffleQuestions,
                ShuffleOptions = vm.ShuffleOptions,
                SubjectIdFilter = vm.SubjectIdFilter,
                RandomMCQ = vm.RandomMCQ,
                RandomTF = vm.RandomTF,
                RandomEssay = vm.RandomEssay
            };

            await _testAdministrationService.UpdateAsync(request, SelectedQuestionIds);
            TempData["Msg"] = "Đã lưu thay đổi.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Delete ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Delete(string id)
        {
            await _testAdministrationService.DeleteAsync(id);
            TempData["Msg"] = "Đã xoá bài test.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Toggle publish ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Publish)]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var (found, message) = await _testAdministrationService.ToggleStatusAsync(id);
            if (!found) return NotFound();
            TempData["Msg"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ---------- Clone ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Clone(string id)
        {
            var cloned = await _testAdministrationService.CloneAsync(id, User.Identity?.Name ?? "system");
            if (!cloned) return NotFound();
            TempData["Msg"] = "Đã nhân bản đề thi (Draft).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Archive ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Archive(string id)
        {
            var archived = await _testAdministrationService.ArchiveAsync(id);
            if (!archived) return NotFound();
            TempData["Msg"] = "Đã chuyển đề thi vào mục Lưu trữ (Archive).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Assign nhanh (1 user) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> AssignToUser(string testId, string userId)
        {
            var (found, message) = await _testAdministrationService.AssignToUserAsync(testId, userId);
            if (!found) return NotFound();
            TempData["Msg"] = message;
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

            var assignData = await _testAdministrationService.GetAssignDataAsync(id, faculty);
            if (assignData == null) return NotFound();

            var vm = new AssignUsersViewModel
            {
                TestId = assignData.TestId,
                TestTitle = assignData.TestTitle,
                Users = assignData.Users,
                AssignedUserIds = assignData.AssignedUserIds,
                Faculties = assignData.Faculties,
                SelectedFaculty = assignData.SelectedFaculty
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
            var (found, message) = await _testAdministrationService.AssignUsersAsync(testId, userIds, startAt, endAt);
            if (!found) return NotFound();
            TempData["Msg"] = message;
            return RedirectToAction(nameof(Assign), new { id = testId });
        }

        // ---------- AssignByFaculty ----------
        [HttpPost("/Tests/AssignByFaculty")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> AssignByFaculty(string testId, string faculty, DateTime? startAt, DateTime? endAt)
        {
            if (string.IsNullOrWhiteSpace(faculty))
            {
                TempData["Err"] = "Vui lòng chọn Lớp/Khoa.";
                return RedirectToAction(nameof(Assign), new { id = testId });
            }

            var (found, message) = await _testAdministrationService.AssignByFacultyAsync(testId, faculty, startAt, endAt);
            if (!found) return NotFound();
            TempData["Msg"] = message;
            return RedirectToAction(nameof(Assign), new { id = testId, faculty });
        }

        // ---------- BulkAssign ----------
        [HttpPost("/Tests/BulkAssign")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> BulkAssign([FromForm] List<string> testIds, string userId, DateTime? startAt, DateTime? endAt)
        {
            TempData["Msg"] = await _testAdministrationService.BulkAssignAsync(testIds, userId, startAt, endAt);
            return RedirectToAction(nameof(Index), new { status = "Draft" });
        }

        // ---------- BulkAssignAuto ----------
        [HttpPost("/Tests/BulkAssignAuto")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> BulkAssignAuto([FromForm] List<string> testIds, DateTime? startAt, DateTime? endAt)
        {
            TempData["Msg"] = await _testAdministrationService.BulkAssignAutoAsync(testIds, startAt, endAt);
            return RedirectToAction(nameof(Index), new { status = "Draft" });
        }
    }
}
