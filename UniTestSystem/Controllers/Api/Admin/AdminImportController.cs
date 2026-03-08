using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Org_Manage)]
[Route("api/admin/import")]
public class AdminImportController : ControllerBase
{
    private readonly IBulkImportService _importService;

    public AdminImportController(IBulkImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("students")]
    public async Task<IActionResult> ImportStudents(IFormFile file, [FromQuery] string? classId)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        
        using var stream = file.OpenReadStream();
        var result = await _importService.ImportStudentsAsync(stream, classId);
        return Ok(result);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> ImportCourses(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();
        var result = await _importService.ImportCoursesAsync(stream);
        return Ok(result);
    }
}
