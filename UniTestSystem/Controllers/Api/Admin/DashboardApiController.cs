using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = UniTestSystem.Domain.User;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IEntityStore<Question> _questions;
    private readonly IEntityStore<Test> _tests;
    private readonly IEntityStore<DomainUser> _users;
    private readonly IEntityStore<Result> _results;

    public DashboardApiController(
        IEntityStore<Question> questions,
        IEntityStore<Test> tests,
        IEntityStore<DomainUser> users,
        IEntityStore<Result> results)
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

