using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DomainUser = UniTestSystem.Domain.User;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/sessions")]
public class AdminSessionsController : ControllerBase
{
    private readonly IEntityStore<Session> _sRepo;
    private readonly IEntityStore<DomainUser> _uRepo;
    private readonly IEntityStore<Test> _tRepo;

    public AdminSessionsController(
        IEntityStore<Session> sRepo,
        IEntityStore<DomainUser> uRepo,
        IEntityStore<Test> tRepo)
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

