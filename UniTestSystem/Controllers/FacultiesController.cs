using UniTestSystem.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Org_Manage)]
    public class FacultiesController : Controller
    {
        private readonly IEntityStore<Faculty> _facultyRepo;
        private readonly IEntityStore<StudentClass> _classRepo;
        private readonly IPermissionService _perms;

        public FacultiesController(IEntityStore<Faculty> facultyRepo, IEntityStore<StudentClass> classRepo, IPermissionService perms)
        { _facultyRepo = facultyRepo; _classRepo = classRepo; _perms = perms; }

        private bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) == true;
        private bool IsStaff => User?.IsInRole(nameof(Role.Staff)) == true;

        private async Task<bool> CanViewAsync()
            => IsAdmin || IsStaff || await _perms.HasAsync(User, PermissionCodes.Org_View);

        private async Task<bool> CanManageAsync()
            => IsAdmin || IsStaff || await _perms.HasAsync(User, PermissionCodes.Org_Manage);

        [HttpGet("/faculties")]
        public async Task<IActionResult> Index()
        {
            if (!await CanViewAsync()) return Redirect("/auth/denied");

            var list = await _facultyRepo.GetAllAsync();
            var classes = await _classRepo.GetAllAsync();
            var counts = classes.GroupBy(t => t.FacultyId ?? "")
                              .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.ClassCounts = counts;
            return View("Index", list.OrderBy(d => d.Name).ToList());
        }

        [HttpGet("/faculties/create")]
        public async Task<IActionResult> Create()
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");
            return View("Create", new Faculty());
        }

        [HttpPost("/faculties/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost([FromForm] Faculty model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Faculty.Name), "Tên khoa tối thiểu 2 ký tự.");
                return View("Create", model);
            }

            var all = await _facultyRepo.GetAllAsync();
            if (all.Any(d => d.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Faculty.Name), "Tên khoa đã tồn tại.");
                return View("Create", model);
            }

            model.Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id;
            model.Name = model.Name.Trim();
            model.Description = model.Description?.Trim();
            model.CreatedAt = DateTime.UtcNow;

            await _facultyRepo.InsertAsync(model);

            TempData["Msg"] = "Đã tạo khoa.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/faculties/edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var d = await _facultyRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                TempData["Err"] = "Không tìm thấy khoa.";
                return RedirectToAction(nameof(Index));
            }
            return View("Edit", d);
        }

        [HttpPost("/faculties/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(string id, [FromForm] Faculty model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var d = await _facultyRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                TempData["Err"] = "Không tìm thấy khoa.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Faculty.Name), "Tên khoa tối thiểu 2 ký tự.");
                return View("Edit", model);
            }

            var all = await _facultyRepo.GetAllAsync();
            if (all.Any(x => x.Id != id && x.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Faculty.Name), "Tên khoa đã tồn tại.");
                return View("Edit", model);
            }

            d.Name = model.Name.Trim();
            d.Description = model.Description?.Trim();
            d.UpdatedAt = DateTime.UtcNow;

            await _facultyRepo.UpsertAsync(x => x.Id == d.Id, d);

            TempData["Msg"] = "Đã cập nhật khoa.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/faculties/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var classes = await _classRepo.GetAllAsync();
            if (classes.Any(t => (t.FacultyId ?? "") == id))
            {
                TempData["Err"] = "Không thể xóa: còn lớp thuộc khoa này.";
                return RedirectToAction(nameof(Index));
            }

            await _facultyRepo.DeleteAsync(x => x.Id == id);
            TempData["Msg"] = "Đã xóa khoa.";
            return RedirectToAction(nameof(Index));
        }
    }
}

