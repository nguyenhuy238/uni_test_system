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
    private readonly IRepository<Faculty> _repo;

    public AdminFacultiesController(IRepository<Faculty> repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return Ok(list.OrderBy(x => x.Name).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Faculty faculty)
    {
        faculty.Id = string.IsNullOrWhiteSpace(faculty.Id) ? Guid.NewGuid().ToString("N") : faculty.Id;
        faculty.CreatedAt = DateTime.UtcNow;
        await _repo.InsertAsync(faculty);
        return Ok(faculty);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Faculty faculty)
    {
        faculty.Id = id;
        faculty.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(x => x.Id == id, faculty);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _repo.DeleteAsync(x => x.Id == id);
        return NoContent();
    }
}
