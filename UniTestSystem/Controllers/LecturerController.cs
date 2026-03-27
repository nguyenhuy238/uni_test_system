using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Lecturer,Admin")]
    public class LecturerController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly ITestAdministrationService _testAdministrationService;

        public LecturerController(IQuestionService questionService, ITestAdministrationService testAdministrationService)
        {
            _questionService = questionService;
            _testAdministrationService = testAdministrationService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var questions = await _questionService.GetAllAsync();
            var tests = await _testAdministrationService.GetAllTestsAsync();

            ViewBag.TotalQuestions = questions.Count;
            ViewBag.TotalTests = tests.Count;
            ViewBag.PendingQuestions = questions.Count(q => q.Status == QuestionStatus.Pending);

            return View();
        }
    }
}

