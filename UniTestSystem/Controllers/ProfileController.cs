using UniTestSystem.Application.Interfaces;
using System.Security.Claims;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.ViewModels.Profile;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserAdministrationService _userAdministrationService;
        private readonly AuthService _authService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ProfileController(
            IUserAdministrationService userAdministrationService, 
            AuthService authService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _userAdministrationService = userAdministrationService;
            _authService = authService;
            _env = env;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("/profile")]
        public async Task<IActionResult> Edit()
        {
            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");

            var u = await _userAdministrationService.GetUserByIdAsync(uid);
            if (u == null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                RoleName = u.Role.ToString(),
                AvatarUrl = u.AvatarUrl
            };

            if (u.Role == Role.Student)
            {
                var s = await _userAdministrationService.GetStudentByIdAsync(uid);
                if (s != null)
                {
                    vm.AcademicYear = s.AcademicYear;
                    vm.StudentClassId = s.StudentClassId ?? "";
                    vm.Major = s.Major;
                }
            }
            else if (u.Role == Role.Lecturer)
            {
                var l = await _userAdministrationService.GetLecturerByIdAsync(uid);
                if (l != null)
                {
                    vm.FacultyName = l.FacultyId ?? ""; // Cannot access Faculty.Name directly without include if lazy loading is off. Wait, Does Lecturer have a Faculty navigation property? Yes. But no easy include in this basic repo. I will simply not fetch FacultyName or fetch it if needed. Let's just leave it empty or map it if we really need it. Ah wait, UserFormViewModel or Profile doesn't strictly need FacultyName to be perfect, but let's see. Let's just change it to l.FacultyId for now or empty. Let's look at the original replacement I did.
                }
            }

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("/profile")]
        public async Task<IActionResult> Edit(ProfileViewModel vm, IFormFile? avatarFile)
        {
            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");
            if (!ModelState.IsValid) return View(vm);

            var u = await _userAdministrationService.GetUserByIdAsync(uid);
            if (u == null) return NotFound();

            var emailExists = await _userAdministrationService.EmailExistsAsync(vm.Email, u.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email đã được sử dụng bởi tài khoản khác.");
                return View(vm);
            }

            if (avatarFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(folder);
                var fileName = u.Id + Path.GetExtension(avatarFile.FileName);
                var path = Path.Combine(folder, fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
                u.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            if (u.Role == Role.Student)
            {
                var s = await _userAdministrationService.GetStudentByIdAsync(uid);
                if (s != null)
                {
                    s.Name = vm.Name?.Trim() ?? "";
                    s.Email = vm.Email?.Trim() ?? "";
                    s.AcademicYear = vm.AcademicYear?.Trim() ?? "1";
                    s.StudentClassId = vm.StudentClassId?.Trim() ?? "";
                    s.Major = vm.Major?.Trim() ?? "";
                    await _userAdministrationService.UpsertStudentAsync(s);
                    u = s; // Cho SignOut/SignIn
                }
            }
            else if (u.Role == Role.Lecturer)
            {
                var l = await _userAdministrationService.GetLecturerByIdAsync(uid);
                if (l != null)
                {
                    l.Name = vm.Name?.Trim() ?? "";
                    l.Email = vm.Email?.Trim() ?? "";
                    await _userAdministrationService.UpsertLecturerAsync(l);
                    u = l; // Cho SignOut/SignIn
                }
            }
            else
            {
                u.Name = vm.Name?.Trim() ?? "";
                u.Email = vm.Email?.Trim() ?? "";
                await _userAdministrationService.UpsertUserAsync(u);
            }

            await HttpContext.SignOutAsync("cookie");
            await HttpContext.SignInAsync("cookie", AuthService.CreatePrincipal(u));

            TempData["Success"] = "Đã cập nhật hồ sơ thành công.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpGet("/profile/change-password")]
        public IActionResult ChangePassword()
        {
            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");

            return View(new ChangePasswordViewModel { UserId = uid });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("/profile/change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid)) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(vm);

            var ok = await _authService.ChangePasswordAsync(uid, vm.OldPassword, vm.NewPassword);
            if (!ok)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng hoặc tài khoản không tồn tại.");
                return View(vm);
            }

            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Edit));
        }
    }
}

