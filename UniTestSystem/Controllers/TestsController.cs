using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
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
        private readonly IAcademicService _academicService;
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IRepository<DifficultyLevel> _difficultyLevelRepo;
        private readonly IRepository<Skill> _skillRepo;

        public TestsController(
            ITestAdministrationService testAdministrationService,
            IQuestionService questionService,
            IAcademicService academicService,
            IRepository<Subject> subjectRepo,
            IRepository<DifficultyLevel> difficultyLevelRepo,
            IRepository<Skill> skillRepo)
        {
            _testAdministrationService = testAdministrationService;
            _questionService = questionService;
            _academicService = academicService;
            _subjectRepo = subjectRepo;
            _difficultyLevelRepo = difficultyLevelRepo;
            _skillRepo = skillRepo;
        }

        // ---------- Index ----------
        public async Task<IActionResult> Index(
            string? status = null,
            string? keyword = null,
            AssessmentType? assessmentType = null,
            string? sort = null)
        {
            var list = (await _testAdministrationService.GetTestsByStatusAsync(status)).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                list = list
                    .Where(t =>
                        (!string.IsNullOrWhiteSpace(t.Title) && t.Title.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(t.Course?.Name) && t.Course.Name.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (assessmentType.HasValue)
            {
                list = list
                    .Where(t => t.AssessmentType == assessmentType.Value)
                    .ToList();
            }

            list = (sort ?? string.Empty).ToLowerInvariant() switch
            {
                "title_asc" => list.OrderBy(t => t.Title).ToList(),
                "title_desc" => list.OrderByDescending(t => t.Title).ToList(),
                "duration_asc" => list.OrderBy(t => t.DurationMinutes).ThenBy(t => t.Title).ToList(),
                "duration_desc" => list.OrderByDescending(t => t.DurationMinutes).ThenBy(t => t.Title).ToList(),
                "published_asc" => list.OrderBy(t => t.PublishedAt ?? DateTime.MinValue).ThenBy(t => t.Title).ToList(),
                _ => list.OrderByDescending(t => t.PublishedAt ?? DateTime.MinValue).ThenBy(t => t.Title).ToList()
            };

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

            var selectedCourseId = !string.IsNullOrWhiteSpace(f.CourseId)
                ? f.CourseId
                : HttpContext.Request.Query["CourseId"].ToString();
            if (!string.IsNullOrWhiteSpace(selectedCourseId))
            {
                f.CourseId = selectedCourseId;
                f.Status = QuestionStatus.Approved;
            }

            var paged = await GetQuestionsForTestContextAsync(f);
            var model = new CreateTestViewModel
            {
                Filter = f,
                Page = paged,
                Title = "",
                CourseId = selectedCourseId,
                DurationMinutes = 10,
                PassScore = 3,
                AssessmentType = AssessmentType.Quiz,
                QuestionSelectionMode = CreateTestViewModel.ManualSelectionMode
            };

            // NEW: giữ giá trị form khi paging (đọc từ query)
            string q(string key) => HttpContext.Request.Query[key].ToString();
            if (!string.IsNullOrWhiteSpace(q("Title"))) model.Title = q("Title");
            if (int.TryParse(q("DurationMinutes"), out var dur)) model.DurationMinutes = dur;
            if (int.TryParse(q("PassScore"), out var pass)) model.PassScore = pass;
            if (Enum.TryParse<AssessmentType>(q("AssessmentType"), true, out var assessmentType)) model.AssessmentType = assessmentType;
            if (!string.IsNullOrWhiteSpace(q("CourseId"))) model.CourseId = q("CourseId");
            if (!string.IsNullOrWhiteSpace(q("QuestionSelectionMode"))) model.QuestionSelectionMode = q("QuestionSelectionMode");
            if (int.TryParse(q("McqCount"), out var mcqCount)) model.McqCount = Math.Max(0, mcqCount);
            if (int.TryParse(q("TfCount"), out var tfCount)) model.TfCount = Math.Max(0, tfCount);
            if (int.TryParse(q("EssayCount"), out var essayCount)) model.EssayCount = Math.Max(0, essayCount);
            if (int.TryParse(q("MatchingCount"), out var matchingCount)) model.MatchingCount = Math.Max(0, matchingCount);
            if (int.TryParse(q("DragDropCount"), out var dragDropCount)) model.DragDropCount = Math.Max(0, dragDropCount);

            await PopulateCoursesAsync(model.CourseId);
            await PopulateQuestionReferenceViewBagsAsync();
            return View(model);
        }


        // ---------- Create (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Create(
            Test t,
            [FromForm] List<string>? SelectedQuestionIds,
            [FromForm] string? QuestionSelectionMode,
            [FromForm] int McqCount,
            [FromForm] int TfCount,
            [FromForm] int EssayCount,
            [FromForm] int MatchingCount,
            [FromForm] int DragDropCount)
        {
            var mode = NormalizeSelectionMode(QuestionSelectionMode);
            var request = new PreviewQuestionsRequest
            {
                CourseId = t.CourseId ?? string.Empty,
                AssessmentType = t.AssessmentType,
                McqCount = Math.Max(0, McqCount),
                TfCount = Math.Max(0, TfCount),
                EssayCount = Math.Max(0, EssayCount),
                MatchingCount = Math.Max(0, MatchingCount),
                DragDropCount = Math.Max(0, DragDropCount)
            };
            var requestedTotal = GetRequestedTotal(request);

            var effectiveSelectedQuestionIds = (SelectedQuestionIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (mode == CreateTestViewModel.RandomByTypeSelectionMode)
            {
                if (string.IsNullOrWhiteSpace(t.CourseId))
                {
                    ModelState.AddModelError(nameof(t.CourseId), "Vui lòng chọn Course trước khi random câu hỏi.");
                }

                if (requestedTotal <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng nhập số lượng câu hỏi random lớn hơn 0.");
                }

                if (ModelState.IsValid)
                {
                    var randomQuestions = await _testAdministrationService.PreviewQuestionsAsync(request);
                    effectiveSelectedQuestionIds = randomQuestions
                        .Select(x => x.Id)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    if (effectiveSelectedQuestionIds.Count == 0)
                    {
                        ModelState.AddModelError(string.Empty, "Không tìm thấy câu hỏi phù hợp theo cấu hình random hiện tại.");
                    }
                    else if (effectiveSelectedQuestionIds.Count < requestedTotal)
                    {
                        TempData["Info"] = $"Cấu hình random yêu cầu {requestedTotal} câu, hệ thống chọn được {effectiveSelectedQuestionIds.Count} câu phù hợp.";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(t.CourseId))
            {
                ModelState.AddModelError(nameof(t.CourseId), "Vui lòng chọn Course trước khi tạo test.");
            }

            if (effectiveSelectedQuestionIds.Any())
            {
                t.TestQuestions = effectiveSelectedQuestionIds
                    .Select(qid => new TestQuestion { QuestionId = qid })
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                var f = new QuestionFilter
                {
                    Page = int.TryParse(HttpContext.Request.Query["Page"], out var p) ? p : 1,
                    PageSize = int.TryParse(HttpContext.Request.Query["PageSize"], out var ps) ? ps : 20,
                    Keyword = HttpContext.Request.Query["Keyword"],
                    CourseId = t.CourseId,
                    Status = QuestionStatus.Approved,
                    Sort = HttpContext.Request.Query["Sort"]
                };
                var psRaw = HttpContext.Request.Query["PageSize"].ToString();
                if (string.Equals(psRaw, "all", StringComparison.OrdinalIgnoreCase))
                    f.PageSize = int.MaxValue;

                var paged = await GetQuestionsForTestContextAsync(f);

                var vm = new CreateTestViewModel
                {
                    Filter = f,
                    Page = paged,
                    Title = t.Title,
                    CourseId = t.CourseId,
                    DurationMinutes = t.DurationMinutes,
                    PassScore = t.PassScore,
                    AssessmentType = t.AssessmentType,
                    QuestionSelectionMode = mode,
                    McqCount = request.McqCount,
                    TfCount = request.TfCount,
                    EssayCount = request.EssayCount,
                    MatchingCount = request.MatchingCount,
                    DragDropCount = request.DragDropCount,
                    SelectedQuestionIds = effectiveSelectedQuestionIds
                };
                await PopulateCoursesAsync(vm.CourseId);
                await PopulateQuestionReferenceViewBagsAsync();
                return View(vm);
            }

            await _testAdministrationService.CreateAndPublishAsync(t, effectiveSelectedQuestionIds);

            TempData["Msg"] = "Đã tạo và publish bài test.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Tests/PreviewQuestions")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> PreviewQuestions([FromBody] PreviewQuestionsRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CourseId))
            {
                return BadRequest(new { message = "Vui lòng chọn Course trước khi preview câu hỏi." });
            }

            var questions = await _testAdministrationService.PreviewQuestionsAsync(request);
            var refs = await LoadQuestionReferenceMapsAsync();
            var warnings = BuildPreviewWarnings(request, questions);
            var warning = warnings.Count > 0
                ? "Không đủ câu hỏi trong pool cho một số loại. Hệ thống đã lấy tối đa theo dữ liệu hiện có."
                : null;

            var result = questions
                .Select((q, index) => new PreviewQuestionItem
                {
                    Stt = index + 1,
                    Id = q.Id,
                    Content = q.Content ?? string.Empty,
                    Type = q.Type.ToString(),
                    Difficulty = ResolveDisplay(refs.DifficultyById, q.DifficultyLevelId),
                    EstimatedPoints = EstimatePoints(q.Type)
                })
                .ToList();

            return Ok(new
            {
                questions = result,
                warnings,
                warning,
                requestedTotal = GetRequestedTotal(request),
                actualTotal = result.Count
            });
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

            var selectedCourseId = !string.IsNullOrWhiteSpace(HttpContext.Request.Query["CourseId"])
                ? HttpContext.Request.Query["CourseId"].ToString()
                : t.CourseId;
            f.CourseId = selectedCourseId;
            if (!string.IsNullOrWhiteSpace(f.CourseId))
                f.Status = QuestionStatus.Approved;

            var paged = await GetQuestionsForTestContextAsync(f);

            var vm = new EditTestViewModel
            {
                Id = t.Id,
                Title = t.Title,
                CourseId = t.CourseId,
                DurationMinutes = t.DurationMinutes,
                PassScore = t.PassScore,
                AssessmentType = t.AssessmentType,
                ShuffleQuestions = t.ShuffleQuestions,
                ShuffleOptions = t.ShuffleOptions,
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
            if (bool.TryParse(q("ShuffleOptions"), out var sho)) vm.ShuffleOptions = sho;
            if (Enum.TryParse<AssessmentType>(q("AssessmentType"), true, out var assessmentType)) vm.AssessmentType = assessmentType;
            if (!string.IsNullOrWhiteSpace(q("CourseId"))) vm.CourseId = q("CourseId");

            await PopulateCoursesAsync(vm.CourseId);
            await PopulateQuestionReferenceViewBagsAsync();
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

            if (string.IsNullOrWhiteSpace(vm.CourseId))
            {
                ModelState.AddModelError(nameof(vm.CourseId), "Vui lòng chọn Course trước khi lưu.");
            }

            if (!ModelState.IsValid)
            {
                var f = new QuestionFilter
                {
                    Page = int.TryParse(HttpContext.Request.Query["Page"], out var p) ? p : 1,
                    PageSize = int.TryParse(HttpContext.Request.Query["PageSize"], out var ps) ? ps : 20,
                    Keyword = HttpContext.Request.Query["Keyword"],
                    CourseId = vm.CourseId,
                    Status = QuestionStatus.Approved,
                    Sort = HttpContext.Request.Query["Sort"]
                };
                var psRaw = HttpContext.Request.Query["PageSize"].ToString();
                if (string.Equals(psRaw, "all", StringComparison.OrdinalIgnoreCase))
                    f.PageSize = int.MaxValue;

                vm.Filter = f;
                vm.Page = await GetQuestionsForTestContextAsync(f);
                vm.SelectedQuestionIds = SelectedQuestionIds ?? new List<string>();
                await PopulateCoursesAsync(vm.CourseId);
                await PopulateQuestionReferenceViewBagsAsync();
                return View(vm);
            }

            var request = new TestUpdateRequest
            {
                Id = vm.Id,
                Title = vm.Title,
                CourseId = vm.CourseId,
                DurationMinutes = vm.DurationMinutes,
                PassScore = vm.PassScore,
                AssessmentType = vm.AssessmentType,
                ShuffleQuestions = vm.ShuffleQuestions,
                ShuffleOptions = vm.ShuffleOptions,
            };

            await _testAdministrationService.UpdateAsync(request, SelectedQuestionIds);
            TempData["Msg"] = "Đã lưu thay đổi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCoursesAsync(string? selectedCourseId)
        {
            var courses = await _academicService.GetAllCoursesAsync();
            var selected = string.IsNullOrWhiteSpace(selectedCourseId) ? null : selectedCourseId;
            ViewBag.Courses = new SelectList(courses, "Id", "Name", selected);
            ViewBag.SelectedCourseName = courses.FirstOrDefault(x => x.Id == selected)?.Name;
        }

        private async Task PopulateQuestionReferenceViewBagsAsync()
        {
            var refs = await LoadQuestionReferenceMapsAsync();
            ViewBag.SubjectNameMap = refs.SubjectById;
            ViewBag.DifficultyNameMap = refs.DifficultyById;
            ViewBag.SkillNameMap = refs.SkillById;
        }

        private async Task<QuestionReferenceMaps> LoadQuestionReferenceMapsAsync()
        {
            var subjects = await _subjectRepo.GetAllAsync(x => !x.IsDeleted);
            var difficulties = await _difficultyLevelRepo.GetAllAsync(x => !x.IsDeleted);
            var skills = await _skillRepo.GetAllAsync(x => !x.IsDeleted);

            return new QuestionReferenceMaps(
                subjects
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase),
                difficulties
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase),
                skills
                    .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase)
            );
        }

        private static string ResolveDisplay(IReadOnlyDictionary<string, string> lookup, string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            return lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : id;
        }

        private async Task<PagedResult<Question>> GetQuestionsForTestContextAsync(QuestionFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.CourseId))
            {
                return new PagedResult<Question>
                {
                    Page = filter.Page <= 0 ? 1 : filter.Page,
                    PageSize = filter.PageSize <= 0 ? 20 : filter.PageSize,
                    Total = 0,
                    Items = new List<Question>()
                };
            }

            filter.Status ??= QuestionStatus.Approved;
            return await _questionService.SearchAsync(filter);
        }

        private void SetFlashMessageByContent(string message)
        {
            if (message.StartsWith("Không thể", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Thiếu", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("Bạn không", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Err"] = message;
                return;
            }

            TempData["Msg"] = message;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        private bool IsPrivilegedCaller()
        {
            return User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());
        }

        private static string NormalizeSelectionMode(string? rawMode)
        {
            return string.Equals(rawMode, CreateTestViewModel.RandomByTypeSelectionMode, StringComparison.OrdinalIgnoreCase)
                ? CreateTestViewModel.RandomByTypeSelectionMode
                : CreateTestViewModel.ManualSelectionMode;
        }

        private static int GetRequestedTotal(PreviewQuestionsRequest request)
        {
            return Math.Max(0, request.McqCount)
                 + Math.Max(0, request.TfCount)
                 + Math.Max(0, request.EssayCount)
                 + Math.Max(0, request.MatchingCount)
                 + Math.Max(0, request.DragDropCount);
        }

        private static List<string> BuildPreviewWarnings(PreviewQuestionsRequest request, IReadOnlyCollection<Question> picked)
        {
            var pickedByType = picked
                .GroupBy(x => x.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var warnings = new List<string>();
            AddMissingWarning(warnings, "MCQ", request.McqCount, pickedByType.GetValueOrDefault(QType.MCQ));
            AddMissingWarning(warnings, "True/False", request.TfCount, pickedByType.GetValueOrDefault(QType.TrueFalse));
            AddMissingWarning(warnings, "Essay", request.EssayCount, pickedByType.GetValueOrDefault(QType.Essay));
            AddMissingWarning(warnings, "Matching", request.MatchingCount, pickedByType.GetValueOrDefault(QType.Matching));
            AddMissingWarning(warnings, "DragDrop", request.DragDropCount, pickedByType.GetValueOrDefault(QType.DragDrop));
            return warnings;
        }

        private static void AddMissingWarning(List<string> warnings, string label, int requested, int actual)
        {
            var safeRequested = Math.Max(0, requested);
            if (safeRequested <= actual)
            {
                return;
            }

            warnings.Add($"{label}: yêu cầu {safeRequested}, chỉ có {actual} câu.");
        }

        private static decimal EstimatePoints(QType type)
        {
            return type switch
            {
                QType.Essay => 2.0m,
                QType.Matching => 1.5m,
                QType.DragDrop => 1.5m,
                _ => 1.0m
            };
        }

        private sealed class PreviewQuestionItem
        {
            public int Stt { get; set; }
            public string Id { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Difficulty { get; set; } = string.Empty;
            public decimal EstimatedPoints { get; set; }
        }

        private sealed record QuestionReferenceMaps(
            IReadOnlyDictionary<string, string> SubjectById,
            IReadOnlyDictionary<string, string> DifficultyById,
            IReadOnlyDictionary<string, string> SkillById);

        // ---------- Delete ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _testAdministrationService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound();
                }

                TempData["Info"] = "Yêu cầu đã được tiếp nhận nhưng không có thay đổi. Tính năng xóa Test đã bị khóa để bảo toàn dữ liệu.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Err"] = ex.Message;
            }

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

        // ---------- Unarchive ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Tests_Create)]
        public async Task<IActionResult> Unarchive(string id)
        {
            var unarchived = await _testAdministrationService.UnarchiveAsync(id);
            if (!unarchived) return NotFound();
            TempData["Msg"] = "Đã chuyển đề thi ra khỏi mục Lưu trữ.";
            return RedirectToAction(nameof(Index), new { status = "Archived" });
        }

        // ---------- Assign (GET) ----------
        [HttpGet("Tests/Assign/{id}")]
        [HttpGet("/Tests/Assign")]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> Assign(string id, [FromQuery] string? classFilter = null, [FromQuery] string? faculty = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var selectedClassId = !string.IsNullOrWhiteSpace(classFilter) ? classFilter : faculty;
            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsPrivilegedCaller();

            var assignData = await _testAdministrationService.GetAssignDataAsync(id, selectedClassId, currentUserId, isPrivileged);
            if (assignData == null)
            {
                TempData["Err"] = "Bạn không có quyền assign bài test này hoặc bài test không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AssignUsersViewModel
            {
                TestId = assignData.TestId,
                TestTitle = assignData.TestTitle,
                CourseName = assignData.CourseName,
                LecturerName = assignData.LecturerName,
                TotalEnrolled = assignData.TotalEnrolled,
                AvailableClasses = assignData.AvailableClasses,
                SelectedClassId = assignData.SelectedClassId,
                IsOwner = assignData.IsOwner,
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
            [FromForm] string? classFilter,
            [FromForm] DateTime? startAt,
            [FromForm] DateTime? endAt)
        {
            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsPrivilegedCaller();

            var (found, message) = await _testAdministrationService.AssignUsersAsync(testId, userIds, startAt, endAt, currentUserId, isPrivileged);
            if (!found) return NotFound();
            SetFlashMessageByContent(message);
            return RedirectToAction(nameof(Assign), new { id = testId, classFilter });
        }

        // ---------- AssignByClass (alias route: AssignByFaculty) ----------
        [HttpPost("/Tests/AssignByClass")]
        [HttpPost("/Tests/AssignByFaculty")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCodes.Exam_Schedule)]
        public async Task<IActionResult> AssignByClass(string testId, string? classId, string? faculty, DateTime? startAt, DateTime? endAt)
        {
            var selectedClassId = !string.IsNullOrWhiteSpace(classId) ? classId : faculty;
            if (string.IsNullOrWhiteSpace(selectedClassId))
            {
                TempData["Err"] = "Vui lòng chọn Lớp/Khoa.";
                return RedirectToAction(nameof(Assign), new { id = testId });
            }

            var currentUserId = GetCurrentUserId();
            var isPrivileged = IsPrivilegedCaller();

            var (found, message) = await _testAdministrationService.AssignByClassAsync(
                testId,
                selectedClassId,
                startAt,
                endAt,
                currentUserId,
                isPrivileged);

            if (!found) return NotFound();
            SetFlashMessageByContent(message);
            return RedirectToAction(nameof(Assign), new { id = testId, classFilter = selectedClassId });
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

    }
}
