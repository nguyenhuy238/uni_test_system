using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Admin,Lecturer")]
    [Route("autotests")]
    public class AutoTestsController : Controller
    {
        [HttpGet("generate")]
        public IActionResult Generate()
        {
            return RedirectToCreateRandom("Chế độ Auto Generate đã được tắt. Vui lòng dùng Create Test với Random theo loại câu hỏi.");
        }

        [HttpPost("generate")]
        [ValidateAntiForgeryToken]
        public IActionResult GeneratePost()
        {
            return RedirectToCreateRandom("Chế độ Auto Generate đã được tắt.");
        }

        [HttpPost("assign-one")]
        [ValidateAntiForgeryToken]
        public IActionResult AssignOne()
        {
            return RedirectToCreateRandom("Chế độ Auto Generate đã được tắt.");
        }

        [HttpPost("assign-all")]
        [ValidateAntiForgeryToken]
        public IActionResult AssignAll()
        {
            return RedirectToCreateRandom("Chế độ Auto Generate đã được tắt.");
        }

        private IActionResult RedirectToCreateRandom(string message)
        {
            TempData["Info"] = message;
            return RedirectToAction("Create", "Tests", new { QuestionSelectionMode = "RandomByType" });
        }
    }
}

