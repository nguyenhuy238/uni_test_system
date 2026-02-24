using Employee_Survey.Application;
using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly IRepository<Team> _teamRepo;
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IPermissionService _perms;

        public TeamsController(IRepository<Team> teamRepo, IRepository<Department> deptRepo, IRepository<User> userRepo, IPermissionService perms)
        { _teamRepo = teamRepo; _deptRepo = deptRepo; _userRepo = userRepo; _perms = perms; }

        private bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) == true;

        private async Task<bool> CanViewAsync()
            => IsAdmin || await _perms.HasAsync(User, PermissionCodes.Org_View);

        private async Task<bool> CanManageAsync()
            => IsAdmin || await _perms.HasAsync(User, PermissionCodes.Org_Manage);

        [HttpGet("/teams")]
        public async Task<IActionResult> Index()
        {
            if (!await CanViewAsync()) return Redirect("/auth/denied");

            var teams = (await _teamRepo.GetAllAsync()).OrderBy(t => t.Name).ToList();
            var depts = await _deptRepo.GetAllAsync();
            var deptMap = depts.ToDictionary(d => d.Id, d => d.Name);
            ViewBag.DeptMap = deptMap;
            return View("Index", teams);
        }

        [HttpGet("/teams/create")]
        public async Task<IActionResult> Create()
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");
            ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
            return View("Create", new Team());
        }

        [HttpPost("/teams/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost([FromForm] Team model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Team.Name), "Tên team tối thiểu 2 ký tự.");
                ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Create", model);
            }

            var all = await _teamRepo.GetAllAsync();
            if (all.Any(t => t.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Team.Name), "Tên team đã tồn tại.");
                ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Create", model);
            }

            model.Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id;
            model.Name = model.Name.Trim();
            model.CreatedAt = DateTime.UtcNow;

            await _teamRepo.InsertAsync(model);

            TempData["Msg"] = "Đã tạo team.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/teams/edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var t = await _teamRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
            {
                TempData["Err"] = "Không tìm thấy team.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
            return View("Edit", t);
        }

        [HttpPost("/teams/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(string id, [FromForm] Team model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var t = await _teamRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
            {
                TempData["Err"] = "Không tìm thấy team.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(Team.Name), "Tên team tối thiểu 2 ký tự.");
                ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Edit", model);
            }

            var all = await _teamRepo.GetAllAsync();
            if (all.Any(x => x.Id != id && x.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Team.Name), "Tên team đã tồn tại.");
                ViewBag.Departments = (await _deptRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Edit", model);
            }

            t.Name = model.Name.Trim();
            t.DepartmentId = model.DepartmentId;
            t.UpdatedAt = DateTime.UtcNow;

            await _teamRepo.UpsertAsync(x => x.Id == t.Id, t);

            TempData["Msg"] = "Đã cập nhật team.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/teams/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var users = await _userRepo.GetAllAsync();
            if (users.Any(u => (u.TeamId ?? "") == id))
            {
                TempData["Err"] = "Không thể xóa: còn nhân viên thuộc team này.";
                return RedirectToAction(nameof(Index));
            }

            await _teamRepo.DeleteAsync(x => x.Id == id);
            TempData["Msg"] = "Đã xóa team.";
            return RedirectToAction(nameof(Index));
        }
    }
}
