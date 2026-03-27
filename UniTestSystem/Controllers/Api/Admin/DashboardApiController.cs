using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ITestAdministrationService _testAdministrationService;
    private readonly IUserAdministrationService _userAdministrationService;
    private readonly IResultsService _resultsService;

    public DashboardApiController(
        IQuestionService questionService,
        ITestAdministrationService testAdministrationService,
        IUserAdministrationService userAdministrationService,
        IResultsService resultsService)
    {
        _questionService = questionService;
        _testAdministrationService = testAdministrationService;
        _userAdministrationService = userAdministrationService;
        _resultsService = resultsService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var questions = await _questionService.GetAllAsync();
        var tests = await _testAdministrationService.GetAllTestsAsync();
        var users = await _userAdministrationService.GetAllUsersAsync();
        var resultCount = await _resultsService.GetResultCountAsync();

        return Ok(new
        {
            TotalQuestions = questions.Count,
            TotalTests = tests.Count,
            TotalUsers = users.Count,
            TotalSubmissions = resultCount,
            ActiveTestCount = tests.Count(t => t.IsPublished)
        });
    }
}

