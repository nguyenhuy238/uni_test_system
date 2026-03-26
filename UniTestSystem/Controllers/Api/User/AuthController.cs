using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DomainUser = UniTestSystem.Domain.User;

namespace UniTestSystem.Controllers.Api.User;

[ApiController]
[Route("api/user/auth")]
public class UserAuthController : ControllerBase
{
    private readonly AuthService _authService;

    public UserAuthController(AuthService authService)
    {
        _authService = authService;
    }

    [EnableRateLimiting("auth-login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateLoginAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (user.Role != Role.Student)
            return Forbid(); // Only Users can login via this endpoint

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var userAgent = Request.Headers["User-Agent"].ToString();
        var session = await _authService.CreateUserSessionAsync(user.Id, userAgent, ipAddress);
        var token = _authService.GenerateJwtToken(user, session.Id);
        return Ok(new { 
            token, 
            user = new { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role 
            } 
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _authService.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "Email already exists" });

        var user = new DomainUser
        {
            Name = request.Name,
            Email = request.Email,
            Role = Role.Student,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _authService.CreateUserAsync(user);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var userAgent = Request.Headers["User-Agent"].ToString();
        var session = await _authService.CreateUserSessionAsync(user.Id, userAgent, ipAddress);
        var token = _authService.GenerateJwtToken(user, session.Id);
        
        return Created("", new { 
            token, 
            user = new { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role 
            } 
        });
    }

}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
