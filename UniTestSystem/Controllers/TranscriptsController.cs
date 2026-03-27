using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class TranscriptsController : Controller
    {
        private readonly ITranscriptService _transcriptService;

        public TranscriptsController(
            ITranscriptService transcriptService)
        {
            _transcriptService = transcriptService;
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
            var file = await _transcriptService.ExportMyTranscriptXlsxAsync(userId);
            return File(file.Content, file.ContentType, file.FileName);
        }

        [Authorize(Roles = "Student")]
        [HttpGet("/transcripts/my/export/pdf")]
        public async Task<IActionResult> ExportMyTranscriptPdf()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var file = await _transcriptService.ExportMyTranscriptPdfAsync(userId);
            return File(file.Content, file.ContentType, file.FileName);
        }

        // Admin/Staff View
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Index(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var page = await _transcriptService.GetAdminTranscriptPageAsync(new TranscriptAdminQuery
            {
                FacultyId = facultyId,
                ClassId = classId,
                Semester = semester
            });

            ViewBag.Faculties = new SelectList(page.Faculties, "Id", "Name", facultyId);
            ViewBag.Classes = new SelectList(page.Classes, "Id", "Name", classId);
            ViewBag.Semesters = new SelectList(page.Semesters, semester);
            ViewBag.SelectedFacultyId = facultyId;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSemester = semester;
            ViewBag.SchoolTranscriptLocked = page.SchoolTranscriptLocked;
            ViewBag.FacultyTranscriptLockMap = page.FacultyTranscriptLockMap;
            ViewBag.SelectedFacultyTranscriptLocked = page.SelectedFacultyTranscriptLocked;

            return View(page.Rows);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/year-end")]
        public async Task<IActionResult> YearEnd(string? academicYear = null, string? facultyId = null)
        {
            var defaultYear = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 1}";
            var selectedAcademicYear = string.IsNullOrWhiteSpace(academicYear) ? defaultYear : academicYear.Trim();

            var preview = await _transcriptService.PreviewYearEndAsync(selectedAcademicYear, facultyId);
            var lookupPage = await _transcriptService.GetAdminTranscriptPageAsync(new TranscriptAdminQuery());

            ViewBag.Faculties = new SelectList(lookupPage.Faculties, "Id", "Name", facultyId);
            ViewBag.SelectedFacultyId = facultyId;
            ViewBag.SelectedAcademicYear = selectedAcademicYear;

            return View(preview);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPost("/transcripts/year-end/finalize")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeYearEnd(string academicYear, string? facultyId, string? returnUrl)
        {
            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            var result = await _transcriptService.FinalizeYearEndAsync(academicYear, facultyId, actor);

            if (result.Success)
            {
                TempData["Msg"] = $"Year-end finalized for {result.AcademicYear}. Finalized students: {result.FinalizedStudents}.";
            }
            else
            {
                TempData["Err"] = result.Messages.Any()
                    ? string.Join(" ", result.Messages)
                    : "Year-end finalization cannot proceed because prerequisites are not satisfied.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(YearEnd), new { academicYear, facultyId });
        }

        // Admin/Staff View details of a specific student
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Details(string id, string? semester = null)
        {
            var details = await _transcriptService.GetStudentTranscriptDetailsAsync(id, semester);
            if (details == null) return NotFound();

            ViewBag.StudentId = details.StudentId;
            ViewBag.StudentName = details.StudentName;
            ViewBag.Summary = details.Summary;
            ViewBag.Semester = details.Semester;
            ViewBag.Semesters = new SelectList(details.Semesters, details.Semester);
            ViewBag.IsTranscriptLocked = details.IsTranscriptLocked;
            ViewBag.SchoolLocked = details.SchoolLocked;
            ViewBag.FacultyLocked = details.FacultyLocked;
            ViewBag.FacultyLockName = details.FacultyLockName;

            return View(details.Grades);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/export/xlsx")]
        public async Task<IActionResult> ExportXlsx(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var file = await _transcriptService.ExportAdminTranscriptOverviewXlsxAsync(new TranscriptAdminQuery
            {
                FacultyId = facultyId,
                ClassId = classId,
                Semester = semester
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/export/pdf")]
        public async Task<IActionResult> ExportPdf(string? facultyId = null, string? classId = null, string? semester = null)
        {
            var file = await _transcriptService.ExportAdminTranscriptOverviewPdfAsync(new TranscriptAdminQuery
            {
                FacultyId = facultyId,
                ClassId = classId,
                Semester = semester
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/{id}/export/xlsx")]
        public async Task<IActionResult> ExportStudentXlsx(string id, string? semester = null)
        {
            var file = await _transcriptService.ExportStudentTranscriptXlsxAsync(id, semester);
            return File(file.Content, file.ContentType, file.FileName);
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet("/transcripts/{id}/export/pdf")]
        public async Task<IActionResult> ExportStudentPdf(string id, string? semester = null)
        {
            var file = await _transcriptService.ExportStudentTranscriptPdfAsync(id, semester);
            return File(file.Content, file.ContentType, file.FileName);
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
            var result = await _transcriptService.FinalizeGradeAsync(new FinalizeGradeCommand
            {
                EnrollmentId = enrollmentId,
                FinalScore = finalScore,
                ExamScore = examScore,
                AssignmentScore = assignmentScore,
                ExamWeight = examWeight,
                AssignmentWeight = assignmentWeight
            });

            if (result.Success)
                TempData["Msg"] = $"Saved final score: {result.ResolvedFinalScore:0.00}";
            else
                TempData["Err"] = result.ErrorMessage;

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

        private IActionResult RedirectToLocalOrIndex(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
    }
}

