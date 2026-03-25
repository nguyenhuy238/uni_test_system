using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Audit_View)]
[Route("api/admin/audit")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditReaderService _auditReader;

    public AdminAuditController(IAuditReaderService auditReader)
    {
        _auditReader = auditReader;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? actor = null,
        [FromQuery] int take = 300)
    {
        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (DateTime.TryParse(from, out var parsedFrom))
        {
            fromUtc = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
        }

        if (DateTime.TryParse(to, out var parsedTo))
        {
            toUtc = DateTime.SpecifyKind(parsedTo, DateTimeKind.Utc);
        }

        var clampedTake = Math.Clamp(take, 1, 2000);
        var logs = await _auditReader.GetAllAsync(fromUtc, toUtc, keyword, actor);
        return Ok(logs.Take(clampedTake).ToList());
    }
}
