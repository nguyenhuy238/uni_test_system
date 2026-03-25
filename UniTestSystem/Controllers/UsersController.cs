using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniTestSystem.ViewModels.Users;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Org_Manage)]
    public class UsersController : Controller
    {
        private readonly IUserAdministrationService _userAdministrationService;

        public UsersController(IUserAdministrationService userAdministrationService)
        {
            _userAdministrationService = userAdministrationService;
        }

        // GET: /Users
        public async Task<IActionResult> Index(string? q, string? classId, Role? role)
        {
            var users = await _userAdministrationService.SearchAsync(q, classId, role);
            var lookups = await _userAdministrationService.GetLookupDataAsync();

            ViewBag.Classes = lookups.Classes;
            ViewBag.Query = q;
            ViewBag.ClassId = classId;
            ViewBag.Role = role;

            return View(users);
        }

        // GET: /Users/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookupsAsync();
            return View(new UserFormViewModel());
        }

        // POST: /Users/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            await LoadLookupsAsync();

            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _userAdministrationService.CreateAsync(ToCommand(vm));
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Tạo người dùng thất bại.");
                return View(vm);
            }

            TempData["Msg"] = "Tạo người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var form = await _userAdministrationService.GetUserFormAsync(id);
            if (form == null) return NotFound();
            await LoadLookupsAsync();

            return View(ToViewModel(form));
        }

        // POST: /Users/Edit/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserFormViewModel vm)
        {
            await LoadLookupsAsync();

            if (!ModelState.IsValid) return View(vm);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "system";
            var result = await _userAdministrationService.UpdateAsync(id, ToCommand(vm), ip);
            if (!result.Found) return NotFound();
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Cập nhật người dùng thất bại.");
                return View(vm);
            }

            TempData["Msg"] = result.Message ?? "Cập nhật người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var (success, error) = await _userAdministrationService.DeleteAsync(id);
            if (!success)
            {
                TempData["Err"] = error ?? "Xóa người dùng thất bại.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Msg"] = "Đã xóa người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/ResetPassword/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var (success, error) = await _userAdministrationService.ResetPasswordAsync(id, newPassword);
            if (!success)
            {
                TempData["Err"] = error ?? "Reset mật khẩu thất bại.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Msg"] = "Đã reset mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLookupsAsync()
        {
            var lookups = await _userAdministrationService.GetLookupDataAsync();
            ViewBag.ClassItems = new SelectList(lookups.Classes, "Id", "Name");
            ViewBag.FacultyItems = new SelectList(lookups.FacultyNames);
            ViewBag.YearItems = new SelectList(lookups.AcademicYears);
        }

        private static UserUpsertCommand ToCommand(UserFormViewModel vm)
        {
            return new UserUpsertCommand
            {
                Name = vm.Name,
                Email = vm.Email,
                Role = vm.Role,
                AcademicYear = vm.AcademicYear,
                StudentClassId = vm.StudentClassId,
                FacultyName = vm.FacultyName,
                Major = vm.Major,
                StudentCode = vm.StudentCode,
                Password = vm.Password
            };
        }

        private static UserFormViewModel ToViewModel(UserFormData form)
        {
            return new UserFormViewModel
            {
                Id = form.Id,
                Name = form.Name,
                Email = form.Email,
                Role = form.Role,
                AcademicYear = form.AcademicYear,
                StudentClassId = form.StudentClassId,
                FacultyName = form.FacultyName,
                Major = form.Major,
                StudentCode = form.StudentCode
            };
        }
    }
}
