using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Transcript_Manage)]
[Route("api/admin/transcripts")]
public sealed class AdminTranscriptsController : ControllerBase
{
    private readonly ITranscriptService _transcriptService;

    public AdminTranscriptsController(ITranscriptService transcriptService)
    {
        _transcriptService = transcriptService;
    }

    [HttpGet("year-end/preview")]
    public async Task<IActionResult> PreviewYearEnd([FromQuery] string academicYear, [FromQuery] string? facultyId = null)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return BadRequest("academicYear is required.");

        var result = await _transcriptService.PreviewYearEndAsync(academicYear, facultyId);
        return Ok(result);
    }

    [HttpPost("year-end/finalize")]
    public async Task<IActionResult> FinalizeYearEnd([FromBody] FinalizeYearEndRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AcademicYear))
            return BadRequest("academicYear is required.");

        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        var result = await _transcriptService.FinalizeYearEndAsync(request.AcademicYear, request.FacultyId, staffId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("year-end/summary")]
    public async Task<IActionResult> GetYearEndSummary([FromQuery] string studentId, [FromQuery] string academicYear)
    {
        if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(academicYear))
            return BadRequest("studentId and academicYear are required.");

        var summary = await _transcriptService.GetYearEndSummaryAsync(studentId, academicYear);
        if (summary == null)
            return NotFound();

        return Ok(summary);
    }
}

public sealed class FinalizeYearEndRequest
{
    public string AcademicYear { get; set; } = "";
    public string? FacultyId { get; set; }
}
