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
    private readonly IRepository<Course> _repo;

    public AdminCoursesController(IRepository<Course> repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync(x => !x.IsDeleted);
        return Ok(list.OrderBy(x => x.Name).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var course = await _repo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        course.Id = string.IsNullOrWhiteSpace(course.Id) ? Guid.NewGuid().ToString("N") : course.Id;
        course.CreatedAt = DateTime.UtcNow;
        await _repo.InsertAsync(course);
        return Ok(course);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Course course)
    {
        course.Id = id;
        course.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(x => x.Id == id, course);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var course = await _repo.FirstOrDefaultAsync(x => x.Id == id);
        if (course != null)
        {
            course.IsDeleted = true;
            course.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(course);
        }
        return NoContent();
    }
}
