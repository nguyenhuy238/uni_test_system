using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers;

[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
public sealed class ProctorController : Controller
{
    private readonly IRepository<Test> _testRepo;

    public ProctorController(IRepository<Test> testRepo)
    {
        _testRepo = testRepo;
    }

    [HttpGet("/proctor")]
    public IActionResult Global()
    {
        ViewBag.ProctorTestId = string.Empty;
        ViewBag.ProctorTestTitle = "Giám sát toàn bộ phòng thi";
        return View("~/Views/Admin/Proctor.cshtml");
    }

    [HttpGet("/proctor/{testId}")]
    public async Task<IActionResult> ByTest(string testId)
    {
        if (string.IsNullOrWhiteSpace(testId))
        {
            return RedirectToAction(nameof(Global));
        }

        var test = await _testRepo.FirstOrDefaultAsync(t => t.Id == testId && !t.IsDeleted);
        if (test == null)
        {
            return NotFound();
        }

        ViewBag.ProctorTestId = testId;
        ViewBag.ProctorTestTitle = test.Title;
        return View("~/Views/Admin/Proctor.cshtml");
    }
}
