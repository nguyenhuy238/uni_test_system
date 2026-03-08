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
        private readonly IRepository<RefreshToken> _refreshTokens;
        private readonly IRepository<UserSession> _userSessions;
        private readonly IConfiguration _cfg;

        public AuthService(
            IRepository<User> users, 
            IRepository<RefreshToken> refreshTokens,
            IRepository<UserSession> userSessions,
            IConfiguration cfg)
        {
            _users = users;
            _refreshTokens = refreshTokens;
            _userSessions = userSessions;
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

            // Check Lockout
            if (u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Account is locked
                return null;
            }

            var hash = u.PasswordHash ?? "";

            // Nhận diện bcrypt ($2a$/$2b$/$2y$). Nếu chưa phải bcrypt, coi như plain → so sánh trực tiếp và NÂNG CẤP lên bcrypt.
            bool isBcrypt = hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$");

            bool ok = isBcrypt
                ? BCrypt.Net.BCrypt.Verify(password, hash)               // đúng cách cho bcrypt
                : password == hash;                                      // tạm hỗ trợ legacy plain

            if (!ok)
            {
                u.AccessFailedCount++;
                if (u.AccessFailedCount >= 5)
                {
                    u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                }
                await _users.UpdateAsync(u);
                return null;
            }

            // AUTO-MIGRATE: nếu trước giờ lưu plain, sau lần login thành công thì hash lại để an toàn
            if (!isBcrypt)
            {
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            // Reset lockout and update login time
            u.AccessFailedCount = 0;
            u.LockoutEnd = null;
            u.LastLoginAt = DateTime.UtcNow;
            
            await _users.UpdateAsync(u);
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

        // --- Refresh Token ---
        public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress)
        {
            var token = new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), // Long random token
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = userId,
                CreatedByIp = ipAddress
            };
            await _refreshTokens.InsertAsync(token);
            return token;
        }

        public async Task<(string jwt, RefreshToken refresh)?> RotateRefreshTokenAsync(string token, string ipAddress)
        {
            var rt = await _refreshTokens.FirstOrDefaultAsync(x => x.Token == token);
            if (rt == null || !rt.IsActive) return null;

            // Revoke old token
            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = ipAddress;
            
            // Create new token
            var newRt = await GenerateRefreshTokenAsync(rt.UserId, ipAddress);
            rt.ReplacedByToken = newRt.Token;
            
            await _refreshTokens.UpdateAsync(rt);

            var user = await _users.FirstOrDefaultAsync(x => x.Id == rt.UserId);
            if (user == null) return null;

            var jwt = GenerateJwtToken(user);
            return (jwt, newRt);
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress)
        {
            var rt = await _refreshTokens.FirstOrDefaultAsync(x => x.Token == token);
            if (rt == null || !rt.IsActive) return;

            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = ipAddress;
            await _refreshTokens.UpdateAsync(rt);
        }

        // --- User Sessions ---
        public async Task<UserSession> CreateUserSessionAsync(string userId, string? userAgent, string? ipAddress)
        {
            var session = new UserSession
            {
                UserId = userId,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            await _userSessions.InsertAsync(session);
            return session;
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(string userId)
        {
            return await _userSessions.GetAllAsync(x => x.UserId == userId && !x.IsRevoked && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow));
        }

        public async Task RevokeSessionAsync(string sessionId)
        {
            var s = await _userSessions.FirstOrDefaultAsync(x => x.Id == sessionId);
            if (s != null)
            {
                s.IsRevoked = true;
                s.RevokedAt = DateTime.UtcNow;
                await _userSessions.UpdateAsync(s);
            }
        }

        public async Task RevokeAllSessionsAsync(string userId)
        {
            var sessions = await _userSessions.GetAllAsync(x => x.UserId == userId && !x.IsRevoked);
            foreach (var s in sessions)
            {
                s.IsRevoked = true;
                s.RevokedAt = DateTime.UtcNow;
                await _userSessions.UpdateAsync(s);
            }
        }
    }
}

