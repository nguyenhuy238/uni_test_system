using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _auth;
        private readonly PasswordResetService _reset;

        public AuthController(AuthService auth, PasswordResetService reset)
        {
            _auth = auth;
            _reset = reset;
        }

        [HttpGet("/auth/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost("/api/auth/login")]
        public async Task<IActionResult> LoginApi([FromBody] LoginRequest request)
        {
            var u = await _auth.ValidateAsync(request.Email, request.Password);
            if (u == null) return Unauthorized(new { message = "Sai email hoặc mật khẩu (hoặc tài khoản bị khóa)" });

            var token = _auth.GenerateJwtToken(u);
            var refreshToken = await _auth.GenerateRefreshTokenAsync(u.Id, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            
            return Ok(new { 
                token, 
                refreshToken = refreshToken.Token,
                user = new { u.Id, u.Name, u.Email, u.Role } 
            });
        }

        public class LoginRequest 
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        [HttpGet("/auth/register")]
        public IActionResult Register() => View();

        [HttpPost("/api/auth/refresh")]
        public async Task<IActionResult> RefreshTokenApi([FromBody] RefreshRequest request)
        {
            var result = await _auth.RotateRefreshTokenAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            if (result == null) return Unauthorized(new { message = "Invalid refresh token" });

            return Ok(new { 
                token = result.Value.jwt, 
                refreshToken = result.Value.refresh.Token 
            });
        }

        public class RefreshRequest
        {
            public string RefreshToken { get; set; } = "";
        }

        [HttpPost("/api/auth/logout")]
        public async Task<IActionResult> LogoutApi([FromBody] RefreshRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                await _auth.RevokeRefreshTokenAsync(
                    request.RefreshToken,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            }

            return Ok(new { message = "Logged out and refresh token revoked." });
        }

        [HttpPost("/auth/register")]
        public async Task<IActionResult> RegisterPost(string name, string email, string role, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View("Register");
            }

            if (!AuthValidation.IsUniversityEmail(email))
            {
                ViewBag.Error = "Vui lòng sử dụng email của trường đại học (@*.edu.vn).";
                return View("Register");
            }

            if (!AuthValidation.IsStrongPassword(password, out var pwdError))
            {
                ViewBag.Error = pwdError;
                return View("Register");
            }

            var existing = await _auth.FindByEmailAsync(email);
            if (existing != null)
            {
                ViewBag.Error = "Email này đã được đăng ký.";
                return View("Register");
            }

            var userRole = Enum.TryParse<Role>(role, out var r) ? r : Role.Student;
            
            User user = userRole switch
            {
                Role.Student => new Student(),
                Role.Lecturer => new Lecturer(),
                _ => new User()
            };

            user.Id = Guid.NewGuid().ToString("N");
            user.Name = name;
            user.Email = email;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Role = userRole;
            user.IsActive = true;

            await _auth.CreateUserAsync(user);
            TempData["Info"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpPost("/auth/login")]
        public async Task<IActionResult> LoginPost(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            var u = await _auth.ValidateAsync(email, password);
            if (u == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu (hoặc tài khoản bị khóa)";
                ViewBag.ReturnUrl = returnUrl;
                return View("Login");
            }

            // Create Session
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var session = await _auth.CreateUserSessionAsync(u.Id, userAgent, ip);

            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync("cookie", AuthService.CreatePrincipal(u), props);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return u.Role switch
            {
                Role.Admin => RedirectToAction("Dashboard", "Admin"),
                Role.Lecturer => RedirectToAction("Dashboard", "Lecturer"),
                Role.Student => RedirectToAction("Index", "MyTests"),
                _ => RedirectToAction("Index", "MyTests")
            };
        }

        [Authorize, HttpPost("/auth/logout")]
        public async Task<IActionResult> Logout()
        {
            // Optional: Revoke current session if we track session ID in claims
            await HttpContext.SignOutAsync("cookie");
            return RedirectToAction("Login");
        }

        [HttpGet("/auth/denied")]
        public IActionResult Denied() => Content("Bạn không có quyền truy cập.");

        // ======= Forgot/Reset password flow giữ nguyên như trước =======

        [HttpGet("/auth/forgot")]
        public IActionResult Forgot() => View();

        [HttpPost("/auth/forgot")]
        public async Task<IActionResult> ForgotPost(string email)
        {
            await _reset.RequestAsync(email);
            TempData["Info"] = "Nếu email tồn tại, mã OTP đã được gửi.";
            return RedirectToAction("Verify", new { email });
        }

        [HttpGet("/auth/verify")]
        public IActionResult Verify(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost("/auth/verify")]
        public async Task<IActionResult> VerifyPost(string email, string otp)
        {
            var token = await _reset.VerifyOtpAsync(email, otp);
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Email = email;
                ViewBag.Error = "OTP không hợp lệ hoặc đã hết hạn.";
                return View("Verify");
            }
            return RedirectToAction("Reset", new { token });
        }

        [HttpGet("/auth/reset")]
        public IActionResult Reset(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        [HttpPost("/auth/reset")]
        public async Task<IActionResult> ResetPost(string token, string password, string confirm)
        {
            if (string.IsNullOrWhiteSpace(password) || password != confirm)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Mật khẩu không khớp.";
                return View("Reset");
            }

            var ok = await _reset.ResetPasswordAsync(token, password);
            if (!ok)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Liên kết đặt lại không hợp lệ hoặc đã hết hạn.";
                return View("Reset");
            }

            TempData["Info"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
    }
}
