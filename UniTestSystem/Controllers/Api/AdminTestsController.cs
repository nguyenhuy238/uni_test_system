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
    private readonly ITestAdministrationService _testAdministrationService;

    public AdminTestsController(ITestAdministrationService testAdministrationService)
    {
        _testAdministrationService = testAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _testAdministrationService.GetAllTestsAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var t = await _testAdministrationService.GetTestByIdAsync(id);
        if (t == null) return NotFound();
        return Ok(t);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Create([FromBody] Test test)
    {
        await _testAdministrationService.CreateRawAsync(test);
        return CreatedAtAction(nameof(GetById), new { id = test.Id }, test);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Update(string id, [FromBody] Test test)
    {
        var updated = await _testAdministrationService.UpdateRawAsync(id, test);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionCodes.Tests_Create)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var deleted = await _testAdministrationService.DeleteRawAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

