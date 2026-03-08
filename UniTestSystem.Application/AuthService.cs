using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace UniTestSystem.Application
{
    public class AuthService
    {
        private readonly IRepository<User> _users;
        private readonly IConfiguration _cfg;

        public AuthService(IRepository<User> users, IConfiguration cfg)
        {
            _users = users;
            _cfg = cfg;
        }

        public async Task<User?> ValidateLoginAsync(string email, string password)
        {
            return await ValidateAsync(email, password);
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task CreateUserAsync(User user)
        {
            await _users.InsertAsync(user);
        }

        public async Task<User?> ValidateAsync(string email, string password)
        {
            var u = await _users.FirstOrDefaultAsync(x => x.Email == email);
            if (u == null) return null;

            var hash = u.PasswordHash ?? "";

            // Nhận diện bcrypt ($2a$/$2b$/$2y$). Nếu chưa phải bcrypt, coi như plain → so sánh trực tiếp và NÂNG CẤP lên bcrypt.
            bool isBcrypt = hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$");

            bool ok = isBcrypt
                ? BCrypt.Net.BCrypt.Verify(password, hash)               // đúng cách cho bcrypt
                : password == hash;                                      // tạm hỗ trợ legacy plain

            if (!ok) return null;

            // AUTO-MIGRATE: nếu trước giờ lưu plain, sau lần login thành công thì hash lại để an toàn
            if (!isBcrypt)
            {
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _users.UpdateAsync(u);
            }

            return u;
        }

        public static ClaimsPrincipal CreatePrincipal(User u)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, u.Id),
                new(ClaimTypes.Name, u.Name),
                new(ClaimTypes.Email, u.Email),
                new(ClaimTypes.Role, u.Role.ToString()),
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie"));
        }

        public async Task<bool> ChangePasswordAsync(string userId, string oldPass, string newPass)
        {
            var u = await _users.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return false;
            if (!BCrypt.Net.BCrypt.Verify(oldPass, u.PasswordHash)) return false;

            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
            await _users.UpdateAsync(u);
            return true;
        }

        public string GenerateJwtToken(User u)
        {
            var jwtKey = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret Key is missing from configuration.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, u.Id),
                new(ClaimTypes.Name, u.Name),
                new(ClaimTypes.Email, u.Email),
                new(ClaimTypes.Role, u.Role.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_cfg["Jwt:ExpireMinutes"] ?? "1440")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

