using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using UniTestSystem.ViewModels.Feedback;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Lecturer,Admin")]
    public class LecturerController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly ITestAdministrationService _testAdministrationService;
        private readonly IResultsService _resultsService;

        public LecturerController(
            IQuestionService questionService,
            ITestAdministrationService testAdministrationService,
            IResultsService resultsService)
        {
            _questionService = questionService;
            _testAdministrationService = testAdministrationService;
            _resultsService = resultsService;
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

        [HttpGet]
        public async Task<IActionResult> Feedbacks(string? testId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            AdminFeedbackListData data;

            if (User.IsInRole(nameof(Role.Admin)))
            {
                data = await _resultsService.GetAdminFeedbacksAsync(testId);
            }
            else
            {
                data = await _resultsService.GetLecturerFeedbacksAsync(currentUserId ?? string.Empty, testId);
            }

            var items = data.Items.Select(x => new AdminFeedbackItemVm
            {
                FeedbackId = x.FeedbackId,
                SessionId = x.SessionId,
                UserName = x.UserName,
                UserEmail = x.UserEmail,
                TestTitle = x.TestTitle,
                CreatedAt = x.CreatedAt,
                Rating = x.Rating,
                Content = x.Content
            }).ToList();

            ViewBag.Tests = data.Tests.Select(t => new { t.Id, t.Title }).ToList();
            ViewBag.SelectedTestId = testId;

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> ExportFeedbacksCsv(string? testId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            AdminFeedbackListData data;

            if (User.IsInRole(nameof(Role.Admin)))
            {
                data = await _resultsService.GetAdminFeedbacksAsync(testId);
            }
            else
            {
                data = await _resultsService.GetLecturerFeedbacksAsync(currentUserId ?? string.Empty, testId);
            }

            var sb = new StringBuilder();
            sb.AppendLine("CreatedAtUtc,TestTitle,StudentName,StudentEmail,Rating,Content,SessionId,FeedbackId");

            foreach (var x in data.Items.OrderByDescending(i => i.CreatedAt))
            {
                sb.AppendLine(
                    $"{x.CreatedAt:O}," +
                    $"{EscapeCsv(x.TestTitle)}," +
                    $"{EscapeCsv(x.UserName)}," +
                    $"{EscapeCsv(x.UserEmail)}," +
                    $"{x.Rating}," +
                    $"{EscapeCsv(x.Content)}," +
                    $"{EscapeCsv(x.SessionId)}," +
                    $"{EscapeCsv(x.FeedbackId)}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"feedbacks-lecturer-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            var safe = (value ?? string.Empty).Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }
}

