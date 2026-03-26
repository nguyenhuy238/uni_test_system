using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class MyTestsController : Controller
    {
        private readonly IUserTestService _userTestService;

        public MyTestsController(IUserTestService userTestService)
        { _userTestService = userTestService; }

        public async Task<IActionResult> Index()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var data = await _userTestService.GetMyTestsOverviewAsync(uid, DateTime.UtcNow);

            ViewBag.InProgress = data.InProgressSessions;
            ViewBag.Submitted = data.SubmittedSessions;
            ViewBag.TestTitles = data.TestTitles;

            return View(data.AvailableTests);
        }
    }
}

