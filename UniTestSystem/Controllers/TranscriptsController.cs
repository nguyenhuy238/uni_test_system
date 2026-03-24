using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class TranscriptsController : Controller
    {
        private readonly ITranscriptService _transcriptService;

        public TranscriptsController(ITranscriptService transcriptService)
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

        // Admin/Staff View
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Index()
        {
            var transcripts = await _transcriptService.GetAllTranscriptsAsync();
            return View(transcripts);
        }

        // Admin/Staff View details of a specific student
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Details(string id)
        {
            var grades = await _transcriptService.GetStudentGradesAsync(id);
            var summary = await _transcriptService.GetStudentTranscriptSummaryAsync(id);
            
            ViewBag.StudentId = id;
            ViewBag.Summary = summary;
            return View(grades);
        }

        // Admin/Staff/Lecturer: Finalize a grade
        [HttpPost]
        [Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
        public async Task<IActionResult> FinalizeGrade(string enrollmentId, decimal finalScore, string returnUrl)
        {
            await _transcriptService.FinalizeCourseGradeAsync(enrollmentId, finalScore);
            return Redirect(returnUrl ?? "/Transcripts/Index");
        }
    }
}
