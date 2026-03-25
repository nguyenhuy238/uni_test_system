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
    private readonly IEntityStore<StudentClass> _repo;

    public AdminClassesController(IEntityStore<StudentClass> repo)
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
    public async Task<IActionResult> Create([FromBody] StudentClass model)
    {
        model.Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id;
        model.CreatedAt = DateTime.UtcNow;
        await _repo.InsertAsync(model);
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] StudentClass model)
    {
        model.Id = id;
        model.UpdatedAt = DateTime.UtcNow;
        await _repo.UpsertAsync(x => x.Id == id, model);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _repo.DeleteAsync(x => x.Id == id);
        return NoContent();
    }
}

