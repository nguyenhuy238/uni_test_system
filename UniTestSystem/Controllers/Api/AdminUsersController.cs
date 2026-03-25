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
    private readonly IRepository<UniTestSystem.Domain.User> _users;

    public AdminUsersController(IRepository<UniTestSystem.Domain.User> users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _users.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var u = await _users.FirstOrDefaultAsync(x => x.Id == id);
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
        await _users.InsertAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UniTestSystem.Domain.User user)
    {
        var existing = await _users.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        if (IsStaffAndProtectedRole(user.Role) || IsStaffAndProtectedRole(existing.Role))
            return Forbid();

        user.Id = id; // Ensure ID consistency
        await _users.UpsertAsync(x => x.Id == id, user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _users.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        if (IsStaffAndProtectedRole(existing.Role))
            return Forbid();

        await _users.DeleteAsync(x => x.Id == id);
        return NoContent();
    }

    private bool IsStaffAndProtectedRole(Role role)
    {
        var isStaffActor = User.IsInRole(Role.Staff.ToString()) && !User.IsInRole(Role.Admin.ToString());
        return isStaffActor && (role == Role.Admin || role == Role.Staff);
    }
}
