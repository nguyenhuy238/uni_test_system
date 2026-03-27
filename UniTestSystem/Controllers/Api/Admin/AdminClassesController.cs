using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Org_Manage)]
[Route("api/admin/classes")]
public class AdminClassesController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public AdminClassesController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _academicService.GetAllClassesAsync();
        return Ok(list.OrderBy(x => x.Name).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudentClass model)
    {
        await _academicService.CreateClassAsync(model);
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] StudentClass model)
    {
        var ok = await _academicService.UpdateClassAsync(id, model);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _academicService.DeleteClassAsync(id);
        return NoContent();
    }
}

