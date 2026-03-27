using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers.Api;

[ApiController]
[Authorize(Policy = PermissionCodes.Users_Manage)]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdministrationService _userAdministrationService;

    public AdminUsersController(IUserAdministrationService userAdministrationService)
    {
        _userAdministrationService = userAdministrationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _userAdministrationService.GetAllUsersAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var u = await _userAdministrationService.GetUserByIdAsync(id);
        if (u == null) return NotFound();
        return Ok(u);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UniTestSystem.Domain.User user)
    {
        if (IsStaffAndProtectedRole(user.Role))
            return Forbid();

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
        }
        await _userAdministrationService.CreateRawAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UniTestSystem.Domain.User user)
    {
        var existing = await _userAdministrationService.GetUserByIdAsync(id);
        if (existing == null) return NotFound();

        if (IsStaffAndProtectedRole(user.Role) || IsStaffAndProtectedRole(existing.Role))
            return Forbid();

        var updated = await _userAdministrationService.UpdateRawAsync(id, user);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _userAdministrationService.GetUserByIdAsync(id);
        if (existing == null) return NotFound();

        if (IsStaffAndProtectedRole(existing.Role))
            return Forbid();

        var deleted = await _userAdministrationService.DeleteRawAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private bool IsStaffAndProtectedRole(Role role)
    {
        var isStaffActor = User.IsInRole(Role.Staff.ToString()) && !User.IsInRole(Role.Admin.ToString());
        return isStaffActor && (role == Role.Admin || role == Role.Staff);
    }
}

