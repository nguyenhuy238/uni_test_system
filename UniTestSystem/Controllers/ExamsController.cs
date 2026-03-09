using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class ExamsController : Controller
    {
        public IActionResult MyTests()
        {
            // Điều hướng đến danh sách bài test dành cho người dùng hiện tại
            return RedirectToAction("Index", "MyTests");
        }
    }
}
