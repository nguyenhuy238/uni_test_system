using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = "RequireStaffOrAdmin")]
    public class StaffController : Controller
    {
        private readonly IExamScheduleService _examScheduleService;
        private readonly ITranscriptService _transcriptService;
        private readonly IGradingService _gradingService;

        public StaffController(
            IExamScheduleService examScheduleService,
            ITranscriptService transcriptService,
            IGradingService gradingService)
        {
            _examScheduleService = examScheduleService;
            _transcriptService = transcriptService;
            _gradingService = gradingService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var examSchedules = await _examScheduleService.GetAllSchedulesAsync();
            var isSchoolLocked = await _transcriptService.IsSchoolTranscriptLockedAsync();
            var facultyLockMap = await _transcriptService.GetFacultyTranscriptLockMapAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var pendingRegradeCount = (await _gradingService.GetPendingRegradeRequestsAsync(userId)).Count;

            ViewBag.TotalExamSchedules = examSchedules.Count;
            ViewBag.LockedTranscriptCount = facultyLockMap.Values.Count(v => v) + (isSchoolLocked ? 1 : 0);
            ViewBag.PendingRegradeCount = pendingRegradeCount;
            ViewBag.IsSchoolTranscriptLocked = isSchoolLocked;
            ViewBag.LockedFacultiesCount = facultyLockMap.Values.Count(v => v);

            return View();
        }
    }
}

