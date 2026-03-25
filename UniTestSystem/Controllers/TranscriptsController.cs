using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class TranscriptsController : Controller
    {
        private readonly ITranscriptService _transcriptService;
        private readonly IEntityStore<Faculty> _facultyRepo;
        private readonly IEntityStore<StudentClass> _classRepo;
        private readonly IEntityStore<Student> _studentRepo;
        private readonly IEntityStore<User> _userRepo;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settingsService;

        public TranscriptsController(
            ITranscriptService transcriptService,
            IEntityStore<Faculty> facultyRepo,
            IEntityStore<StudentClass> classRepo,
            IEntityStore<Student> studentRepo,
            IEntityStore<User> userRepo,
            IExportService exportService,
            ISettingsService settingsService)
        {
            _transcriptService = transcriptService;
            _facultyRepo = facultyRepo;
            _classRepo = classRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _exportService = exportService;
            _settingsService = settingsService;
        }

        // Student View
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyTranscript()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var grades = await _transcriptService.GetStudentGradesAsync(userId);
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(userId);
            
            ViewBag.Summary = summary;
            return View(grades);
        }

        [Authorize(Roles = "Student")]
        [HttpGet("/transcripts/my/export/xlsx")]
        public async Task<IActionResult> ExportMyTranscriptXlsx()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var grades = await _transcriptService.GetStudentGradesAsync(userId);
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(userId);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var bytes = _exportService.ExportStudentTranscriptExcel(grades, summary, user?.Name ?? userId, userId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"my-transcript-{userId}.xlsx");
        }

        [Authorize(Roles = "Student")]
        [HttpGet("/transcripts/my/export/pdf")]
        public async Task<IActionResult> ExportMyTranscriptPdf()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var grades = await _transcriptService.GetStudentGradesAsync(userId);
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(userId);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var settings = await _settingsService.GetAsync();
            var bytes = _exportService.ExportStudentTranscriptPdf(grades, summary, user?.Name ?? userId, userId, settings);
            return File(bytes, "application/pdf", $"my-transcript-{userId}.pdf");
        }

        // Admin/Staff View
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Index(string? facultyId = null, string? classId = null, string? semester = null)
        {
            await PrepareFiltersAsync(facultyId, classId, semester);
            await PrepareLockStateAsync(facultyId);
            var rows = await _transcriptService.GetAdminTranscriptRowsAsync(facultyId, classId, semester);
            return View(rows);
        }

        // Admin/Staff View details of a specific student
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Details(string id, string? semester = null)
        {
            var all = await _transcriptService.GetStudentGradesAsync(id);
            var grades = string.IsNullOrWhiteSpace(semester)
                ? all
                : all.Where(x => string.Equals(x.Semester, semester, StringComparison.OrdinalIgnoreCase)).ToList();
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(id);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            
            ViewBag.StudentId = id;
            ViewBag.StudentName = user?.Name ?? id;
            ViewBag.Summary = summary;
            ViewBag.Semester = semester;
            ViewBag.Semesters = new SelectList(await _transcriptService.GetAvailableSemestersAsync(), semester);

            var schoolLocked = await _transcriptService.IsSchoolTranscriptLockedAsync();
            var student = await _studentRepo.FirstOrDefaultAsync(x => x.Id == id);
            var facultyLocked = false;
            string? facultyName = null;
            if (!string.IsNullOrWhiteSpace(student?.StudentClassId))
            {
                var studentClass = await _classRepo.FirstOrDefaultAsync(x => x.Id == student.StudentClassId);
                if (!string.IsNullOrWhiteSpace(studentClass?.FacultyId))
                {
                    facultyLocked = await _transcriptService.IsFacultyTranscriptLockedAsync(studentClass.FacultyId);
                    facultyName = (await _facultyRepo.FirstOrDefaultAsync(x => x.Id == studentClass.FacultyId))?.Name;
                }
            }

            ViewBag.IsTranscriptLocked = schoolLocked || facultyLocked;
            ViewBag.SchoolLocked = schoolLocked;
            ViewBag.FacultyLocked = facultyLocked;
            ViewBag.FacultyLockName = facultyName;
            return View(grades);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/export/xlsx")]
        public async Task<IActionResult> ExportXlsx(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var rows = await _transcriptService.GetAdminTranscriptRowsAsync(facultyId, classId, semester);
            var faculty = string.IsNullOrWhiteSpace(facultyId) ? null : await _facultyRepo.FirstOrDefaultAsync(x => x.Id == facultyId);
            var studentClass = string.IsNullOrWhiteSpace(classId) ? null : await _classRepo.FirstOrDefaultAsync(x => x.Id == classId);
            var bytes = _exportService.ExportTranscriptOverviewExcel(rows, faculty?.Name, studentClass?.Name, semester);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"transcripts-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/export/pdf")]
        public async Task<IActionResult> ExportPdf(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var rows = await _transcriptService.GetAdminTranscriptRowsAsync(facultyId, classId, semester);
            var faculty = string.IsNullOrWhiteSpace(facultyId) ? null : await _facultyRepo.FirstOrDefaultAsync(x => x.Id == facultyId);
            var studentClass = string.IsNullOrWhiteSpace(classId) ? null : await _classRepo.FirstOrDefaultAsync(x => x.Id == classId);
            var settings = await _settingsService.GetAsync();
            var bytes = _exportService.ExportTranscriptOverviewPdf(rows, settings, faculty?.Name, studentClass?.Name, semester);
            return File(bytes, "application/pdf", $"transcripts-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/{id}/export/xlsx")]
        public async Task<IActionResult> ExportStudentXlsx(string id, string? semester = null)
        {
            var all = await _transcriptService.GetStudentGradesAsync(id);
            var grades = string.IsNullOrWhiteSpace(semester)
                ? all
                : all.Where(x => string.Equals(x.Semester, semester, StringComparison.OrdinalIgnoreCase)).ToList();
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(id);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            var bytes = _exportService.ExportStudentTranscriptExcel(grades, summary, user?.Name ?? id, id);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"student-transcript-{id}.xlsx");
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/{id}/export/pdf")]
        public async Task<IActionResult> ExportStudentPdf(string id, string? semester = null)
        {
            var all = await _transcriptService.GetStudentGradesAsync(id);
            var grades = string.IsNullOrWhiteSpace(semester)
                ? all
                : all.Where(x => string.Equals(x.Semester, semester, StringComparison.OrdinalIgnoreCase)).ToList();
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(id);
            var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            var settings = await _settingsService.GetAsync();
            var bytes = _exportService.ExportStudentTranscriptPdf(grades, summary, user?.Name ?? id, id, settings);
            return File(bytes, "application/pdf", $"student-transcript-{id}.pdf");
        }

        // Admin/Staff/Lecturer: Finalize a grade
        [HttpPost]
        [Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
        public async Task<IActionResult> FinalizeGrade(
            string enrollmentId,
            decimal? finalScore,
            decimal? examScore,
            decimal? assignmentScore,
            decimal? examWeight,
            decimal? assignmentWeight,
            string returnUrl)
        {
            try
            {
                decimal resolvedFinalScore;
                if (examScore.HasValue || assignmentScore.HasValue)
                {
                    if (!examScore.HasValue || !assignmentScore.HasValue)
                        throw new Exception("Both exam score and assignment score are required when weighted formula is used.");

                    resolvedFinalScore = _transcriptService.CalculateWeightedFinalScore(
                        assignmentScore.Value,
                        examScore.Value,
                        assignmentWeight ?? 30m,
                        examWeight ?? 70m);
                }
                else if (finalScore.HasValue)
                {
                    resolvedFinalScore = finalScore.Value;
                }
                else
                {
                    throw new Exception("Final score is required.");
                }

                await _transcriptService.FinalizeCourseGradeAsync(enrollmentId, resolvedFinalScore);
                TempData["Msg"] = $"Saved final score: {resolvedFinalScore:0.00}";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                returnUrl = "/Transcripts/Index";

            return Redirect(returnUrl);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPost("/transcripts/lock/school")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockSchool(string? note, string? returnUrl)
        {
            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            await _transcriptService.LockSchoolTranscriptAsync(actor, note);
            TempData["Msg"] = "School transcript is now locked.";
            return RedirectToLocalOrIndex(returnUrl);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPost("/transcripts/unlock/school")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockSchool(string? note, string? returnUrl)
        {
            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            await _transcriptService.UnlockSchoolTranscriptAsync(actor, note);
            TempData["Msg"] = "School transcript lock removed.";
            return RedirectToLocalOrIndex(returnUrl);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPost("/transcripts/lock/faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockFaculty(string facultyId, string? note, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                TempData["Err"] = "Faculty is required.";
                return RedirectToLocalOrIndex(returnUrl);
            }

            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            await _transcriptService.LockFacultyTranscriptAsync(facultyId, actor, note);
            TempData["Msg"] = "Faculty transcript is now locked.";
            return RedirectToLocalOrIndex(returnUrl);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPost("/transcripts/unlock/faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockFaculty(string facultyId, string? note, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(facultyId))
            {
                TempData["Err"] = "Faculty is required.";
                return RedirectToLocalOrIndex(returnUrl);
            }

            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            await _transcriptService.UnlockFacultyTranscriptAsync(facultyId, actor, note);
            TempData["Msg"] = "Faculty transcript lock removed.";
            return RedirectToLocalOrIndex(returnUrl);
        }

        private async Task PrepareFiltersAsync(string? facultyId, string? classId, string? semester)
        {
            var faculties = await _facultyRepo.GetAllAsync(x => !x.IsDeleted);
            var classes = await _classRepo.GetAllAsync(x => !x.IsDeleted);
            var semesters = await _transcriptService.GetAvailableSemestersAsync();

            ViewBag.Faculties = new SelectList(faculties.OrderBy(f => f.Name), "Id", "Name", facultyId);
            ViewBag.Classes = new SelectList(classes.OrderBy(c => c.Name), "Id", "Name", classId);
            ViewBag.Semesters = new SelectList(semesters, semester);
            ViewBag.SelectedFacultyId = facultyId;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSemester = semester;
        }

        private async Task PrepareLockStateAsync(string? selectedFacultyId)
        {
            var schoolLocked = await _transcriptService.IsSchoolTranscriptLockedAsync();
            var facultyLockMap = await _transcriptService.GetFacultyTranscriptLockMapAsync();

            var selectedFacultyLocked = false;
            if (!string.IsNullOrWhiteSpace(selectedFacultyId))
                selectedFacultyLocked = facultyLockMap.TryGetValue(selectedFacultyId, out var locked) && locked;

            ViewBag.SchoolTranscriptLocked = schoolLocked;
            ViewBag.FacultyTranscriptLockMap = facultyLockMap;
            ViewBag.SelectedFacultyTranscriptLocked = selectedFacultyLocked;
        }

        private IActionResult RedirectToLocalOrIndex(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
    }
}

