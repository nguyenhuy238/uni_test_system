using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = Employee_Survey.Domain.User;

namespace Employee_Survey.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/admin/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IRepository<Question> _questions;
    private readonly IRepository<Test> _tests;
    private readonly IRepository<DomainUser> _users;
    private readonly IRepository<Result> _results;

    public DashboardApiController(
        IRepository<Question> questions,
        IRepository<Test> tests,
        IRepository<DomainUser> users,
        IRepository<Result> results)
    {
        _questions = questions;
        _tests = tests;
        _users = users;
        _results = results;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var questionCount = (await _questions.GetAllAsync()).Count;
        var testCount = (await _tests.GetAllAsync()).Count;
        var userCount = (await _users.GetAllAsync()).Count;
        var resultCount = (await _results.GetAllAsync()).Count;

        return Ok(new
        {
            TotalQuestions = questionCount,
            TotalTests = testCount,
            TotalUsers = userCount,
            TotalSubmissions = resultCount,
            ActiveTestCount = (await _tests.GetAllAsync()).Count(t => t.IsPublished)
        });
    }
}
