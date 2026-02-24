using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = Employee_Survey.Domain.User;

namespace Employee_Survey.Controllers.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/admin/sessions")]
public class AdminSessionsController : ControllerBase
{
    private readonly IRepository<Session> _sRepo;
    private readonly IRepository<DomainUser> _uRepo;
    private readonly IRepository<Test> _tRepo;

    public AdminSessionsController(
        IRepository<Session> sRepo,
        IRepository<DomainUser> uRepo,
        IRepository<Test> tRepo)
    {
        _sRepo = sRepo;
        _uRepo = uRepo;
        _tRepo = tRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sessions = await _sRepo.GetAllAsync();
        var users = await _uRepo.GetAllAsync();
        var tests = await _tRepo.GetAllAsync();

        var result = sessions.Select(s => {
            var user = users.FirstOrDefault(u => u.Id == s.UserId);
            var test = tests.FirstOrDefault(t => t.Id == s.TestId);
            return new {
                s.Id,
                s.UserId,
                UserName = user?.Name ?? "Unknown",
                UserEmail = user?.Email,
                s.TestId,
                TestTitle = test?.Title ?? "Unknown",
                s.StartAt,
                s.EndAt,
                s.Status,
                s.LastActivityAt,
                s.TotalScore,
                s.MaxScore,
                s.Percent,
                s.IsPassed
            };
        }).OrderByDescending(s => s.LastActivityAt).ToList();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Terminate(string id)
    {
        var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        // If it's active, we could force-complete it, but here let's just delete
        await _sRepo.DeleteAsync(x => x.Id == id);
        return Ok(new { message = "Session terminated and deleted." });
    }
}
