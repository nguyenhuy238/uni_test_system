using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Org_Manage)]
[Route("api/admin/faculties")]
public class AdminFacultiesController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public AdminFacultiesController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _academicService.GetAllFacultiesAsync();
        return Ok(list.OrderBy(x => x.Name).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Faculty faculty)
    {
        await _academicService.CreateFacultyAsync(faculty);
        return Ok(faculty);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Faculty faculty)
    {
        var ok = await _academicService.UpdateFacultyAsync(id, faculty);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _academicService.SoftDeleteFacultyAsync(id);
        return NoContent();
    }
}

