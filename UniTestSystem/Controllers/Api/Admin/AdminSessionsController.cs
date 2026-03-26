using UniTestSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
[Route("api/admin/sessions")]
public class AdminSessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public AdminSessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _sessionService.GetAdminSessionsAsync());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Terminate(string id)
    {
        var terminated = await _sessionService.TerminateSessionAsync(id);
        if (!terminated) return NotFound();
        return Ok(new { message = "Session terminated and deleted." });
    }
}

