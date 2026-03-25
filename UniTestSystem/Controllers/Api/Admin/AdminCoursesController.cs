using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.Org_Manage)]
[Route("api/admin/courses")]
public class AdminCoursesController : ControllerBase
{
    private readonly IAcademicService _academicService;

    public AdminCoursesController(IAcademicService academicService)
    {
        _academicService = academicService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _academicService.GetAllCoursesAsync();
        return Ok(list.OrderBy(x => x.Name).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var course = await _academicService.GetCourseByIdAsync(id);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        var ok = await _academicService.CreateCourseAsync(course);
        if (!ok) return BadRequest();

        return Ok(course);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Course course)
    {
        var ok = await _academicService.UpdateCourseAsync(id, course);
        if (!ok) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _academicService.DeleteCourseAsync(id);
        return NoContent();
    }
}
