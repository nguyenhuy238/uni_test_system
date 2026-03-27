using UniTestSystem.Application.Interfaces;
// UniTestSystem.Controllers/FeedbackController.cs
using System.Security.Claims;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.ViewModels.Feedback;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly IResultsService _resultsService;

        public FeedbackController(IResultsService resultsService)
        {
            _resultsService = resultsService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET /Feedback/Create?sessionId=...
        [HttpGet]
        public async Task<IActionResult> Create(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest("Thiếu sessionId.");

            var ctx = await _resultsService.GetFeedbackCreateContextAsync(sessionId, CurrentUserId ?? string.Empty);
            if (ctx.Status == FeedbackAccessStatus.NotFound) return NotFound("Session không tồn tại.");
            if (ctx.Status == FeedbackAccessStatus.Forbidden) return Forbid();
            if (ctx.Status == FeedbackAccessStatus.InProgress) return BadRequest("Bạn cần nộp bài trước khi gửi feedback.");
            if (!string.IsNullOrWhiteSpace(ctx.ExistingFeedbackId))
                return RedirectToAction(nameof(Edit), new { id = ctx.ExistingFeedbackId });

            var vm = new FeedbackCreateViewModel
            {
                SessionId = sessionId,
                TestTitle = ctx.TestTitle
            };
            return View(vm);
        }

        // POST /Feedback/Create
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(FeedbackCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var result = await _resultsService.CreateFeedbackAsync(vm.SessionId, CurrentUserId ?? string.Empty, vm.Content, vm.Rating);
            if (result.Status == FeedbackCommandStatus.NotFound) return NotFound("Session không tồn tại.");
            if (result.Status == FeedbackCommandStatus.Forbidden) return Forbid();
            if (result.Status == FeedbackCommandStatus.InProgress)
            {
                ModelState.AddModelError("", "Bạn cần nộp bài trước khi gửi feedback.");
                return View(vm);
            }
            if (result.Status == FeedbackCommandStatus.Conflict && !string.IsNullOrWhiteSpace(result.ExistingFeedbackId))
                return RedirectToAction(nameof(Edit), new { id = result.ExistingFeedbackId });

            TempData["Msg"] = "Cảm ơn bạn đã gửi feedback!";
            return RedirectToAction("Result", "Sessions", new { id = result.SessionId });
        }

        // GET /Feedback/Edit/{id}
        [HttpGet("/Feedback/Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var ctx = await _resultsService.GetFeedbackEditContextAsync(id, CurrentUserId ?? string.Empty);
            if (ctx.Status == FeedbackAccessStatus.NotFound) return NotFound();
            if (ctx.Status == FeedbackAccessStatus.Forbidden) return Forbid();

            var vm = new FeedbackCreateViewModel
            {
                SessionId = ctx.SessionId,
                Content = ctx.Content,
                Rating = ctx.Rating,
                TestTitle = ctx.TestTitle
            };
            ViewBag.FeedbackId = id;
            return View(vm);
        }

        // POST /Feedback/Edit/{id}
        [ValidateAntiForgeryToken]
        [HttpPost("/Feedback/Edit/{id}")]
        public async Task<IActionResult> Edit(string id, FeedbackCreateViewModel vm)
        {
            if (!ModelState.IsValid) { ViewBag.FeedbackId = id; return View(vm); }

            var result = await _resultsService.UpdateFeedbackAsync(id, CurrentUserId ?? string.Empty, vm.Content, vm.Rating);
            if (result.Status == FeedbackCommandStatus.NotFound) return NotFound();
            if (result.Status == FeedbackCommandStatus.Forbidden) return Forbid();

            TempData["Msg"] = "Đã cập nhật feedback.";
            return RedirectToAction("Result", "Sessions", new { id = result.SessionId });
        }
    }
}

