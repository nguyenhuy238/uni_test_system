using System.ComponentModel.DataAnnotations;
using Employee_Survey.Application;
using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<Team> _teamRepo;
        private readonly IPermissionService _perms;

        public DepartmentsController(IRepository<Department> deptRepo, IRepository<Team> teamRepo, IPermissionService perms)
        { _deptRepo = deptRepo; _teamRepo = teamRepo; _perms = perms; }

        private bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) == true;

        private async Task<bool> CanViewAsync()
            => IsAdmin || await _perms.HasAsync(User, PermissionCodes.Org_View);

        private async Task<bool> CanManageAsync()
            => IsAdmin || await _perms.HasAsync(User, PermissionCodes.Org_Manage);

        [HttpGet("/departments")]
        public async Task<IActionResult> Index()
        {
            if (!await CanViewAsync()) return Redirect("/auth/denied");

            var list = await _deptRepo.GetAllAsync();
            var teams = await _teamRepo.GetAllAsync();
            var counts = teams.GroupBy(t => t.DepartmentId ?? "")
                              .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.TeamCounts = counts;
            return View("Index", list.OrderBy(d => d.Name).ToList());
        }

        [HttpGet("/departments/create")]
        public async Task<IActionResult> Create()
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");
            return View("Create", new Department());
        }

        [HttpPost("/departments/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost([FromForm] Department model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Department.Name), "Tên phòng ban tối thiểu 2 ký tự.");
                return View("Create", model);
            }

            var all = await _deptRepo.GetAllAsync();
            if (all.Any(d => d.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Department.Name), "Tên phòng ban đã tồn tại.");
                return View("Create", model);
            }

            model.Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id;
            model.Name = model.Name.Trim();
            model.Description = model.Description?.Trim();
            model.CreatedAt = DateTime.UtcNow;

            await _deptRepo.InsertAsync(model);

            TempData["Msg"] = "Đã tạo phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/departments/edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var d = await _deptRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                TempData["Err"] = "Không tìm thấy phòng ban.";
                return RedirectToAction(nameof(Index));
            }
            return View("Edit", d);
        }

        [HttpPost("/departments/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(string id, [FromForm] Department model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var d = await _deptRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (d == null)
            {
                TempData["Err"] = "Không tìm thấy phòng ban.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Department.Name), "Tên phòng ban tối thiểu 2 ký tự.");
                return View("Edit", model);
            }

            var all = await _deptRepo.GetAllAsync();
            if (all.Any(x => x.Id != id && x.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Department.Name), "Tên phòng ban đã tồn tại.");
                return View("Edit", model);
            }

            d.Name = model.Name.Trim();
            d.Description = model.Description?.Trim();
            d.UpdatedAt = DateTime.UtcNow;

            await _deptRepo.UpsertAsync(x => x.Id == d.Id, d);

            TempData["Msg"] = "Đã cập nhật phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/departments/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var teams = await _teamRepo.GetAllAsync();
            if (teams.Any(t => (t.DepartmentId ?? "") == id))
            {
                TempData["Err"] = "Không thể xóa: còn team thuộc phòng ban này.";
                return RedirectToAction(nameof(Index));
            }

            await _deptRepo.DeleteAsync(x => x.Id == id);
            TempData["Msg"] = "Đã xóa phòng ban.";
            return RedirectToAction(nameof(Index));
        }
    }
}
