using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api;

[ApiController]
[Authorize(Policy = PermissionCodes.Tests_View)]
[Route("api/admin/tests")]
public class AdminTestsController : ControllerBase
{
    private readonly IRepository<Test> _tests;

    public AdminTestsController(IRepository<Test> tests)
    {
        _tests = tests;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _tests.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var t = await _tests.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        return Ok(t);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Create([FromBody] Test test)
    {
        await _tests.InsertAsync(test);
        return CreatedAtAction(nameof(GetById), new { id = test.Id }, test);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Update(string id, [FromBody] Test test)
    {
        var existing = await _tests.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        test.Id = id;
        await _tests.UpsertAsync(x => x.Id == id, test);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Delete(string id)
    {
        await _tests.DeleteAsync(x => x.Id == id);
        return NoContent();
    }
}
