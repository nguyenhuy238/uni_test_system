using Employee_Survey.Application;
using Employee_Survey.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DomainUser = Employee_Survey.Domain.User;

namespace Employee_Survey.Controllers.Api.User;

[ApiController]
[Route("api/user/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(AuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateLoginAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (user.Role != Role.User)
            return Forbid(); // Only Users can login via this endpoint

        var token = GenerateJwtToken(user);
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
            Role = Role.User,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _authService.CreateUserAsync(user);
        var token = GenerateJwtToken(user);
        
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

    private string GenerateJwtToken(DomainUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_secret_key_1234567890123456"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
