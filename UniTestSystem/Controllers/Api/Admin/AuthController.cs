using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Application;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AdminAuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateLoginAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (user.Role != Role.Admin && user.Role != Role.Lecturer && user.Role != Role.Staff)
            return Forbid(); // Only Admin, Lecturer and Staff can login via this endpoint

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var token = _authService.GenerateJwtToken(user);
        var refreshToken = await _authService.GenerateRefreshTokenAsync(user.Id, ip);

        return Ok(new
        {
            token,
            refreshToken = refreshToken.Token,
            user = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var rotated = await _authService.RotateRefreshTokenAsync(request.RefreshToken, ip);
        if (rotated == null)
        {
            return Unauthorized(new { message = "Invalid refresh token." });
        }

        return Ok(new
        {
            token = rotated.Value.jwt,
            refreshToken = rotated.Value.refresh.Token
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
        }

        return Ok(new { message = "Logged out." });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = "";
}
