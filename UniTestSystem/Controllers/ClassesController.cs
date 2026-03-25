using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Org_Manage)]
    public class ClassesController : Controller
    {
        private readonly IEntityStore<StudentClass> _classRepo;
        private readonly IEntityStore<Faculty> _facultyRepo;
        private readonly IEntityStore<Student> _studentRepo;
        private readonly IPermissionService _perms;

        public ClassesController(IEntityStore<StudentClass> classRepo, IEntityStore<Faculty> facultyRepo, IEntityStore<Student> studentRepo, IPermissionService perms)
        { _classRepo = classRepo; _facultyRepo = facultyRepo; _studentRepo = studentRepo; _perms = perms; }

        private bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) == true;
        private bool IsStaff => User?.IsInRole(nameof(Role.Staff)) == true;

        private async Task<bool> CanViewAsync()
            => IsAdmin || IsStaff || await _perms.HasAsync(User, PermissionCodes.Org_View);

        private async Task<bool> CanManageAsync()
            => IsAdmin || IsStaff || await _perms.HasAsync(User, PermissionCodes.Org_Manage);

        [HttpGet("/classes")]
        public async Task<IActionResult> Index()
        {
            if (!await CanViewAsync()) return Redirect("/auth/denied");

            var classes = (await _classRepo.GetAllAsync()).OrderBy(t => t.Name).ToList();
            var faculties = await _facultyRepo.GetAllAsync();
            var facultyMap = faculties.ToDictionary(d => d.Id, d => d.Name);
            ViewBag.FacultyMap = facultyMap;
            return View("Index", classes);
        }

        [HttpGet("/classes/create")]
        public async Task<IActionResult> Create()
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");
            ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
            return View("Create", new StudentClass());
        }

        [HttpPost("/classes/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost([FromForm] StudentClass model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(StudentClass.Name), "Tên lớp tối thiểu 2 ký tự.");
                ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Create", model);
            }

            var all = await _classRepo.GetAllAsync();
            if (all.Any(t => t.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(StudentClass.Name), "Tên lớp đã tồn tại.");
                ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Create", model);
            }

            model.Id = string.IsNullOrWhiteSpace(model.Id) ? Guid.NewGuid().ToString("N") : model.Id;
            model.Name = model.Name.Trim();
            model.CreatedAt = DateTime.UtcNow;

            await _classRepo.InsertAsync(model);

            TempData["Msg"] = "Đã tạo lớp.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/classes/edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var t = await _classRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
            {
                TempData["Err"] = "Không tìm thấy lớp.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
            return View("Edit", t);
        }

        [HttpPost("/classes/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(string id, [FromForm] StudentClass model)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var t = await _classRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
            {
                TempData["Err"] = "Không tìm thấy lớp.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length < 2)
            {
                ModelState.AddModelError(nameof(StudentClass.Name), "Tên lớp tối thiểu 2 ký tự.");
                ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Edit", model);
            }

            var all = await _classRepo.GetAllAsync();
            if (all.Any(x => x.Id != id && x.Name.Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(StudentClass.Name), "Tên lớp đã tồn tại.");
                ViewBag.Faculties = (await _facultyRepo.GetAllAsync()).OrderBy(d => d.Name).ToList();
                return View("Edit", model);
            }

            t.Name = model.Name.Trim();
            t.FacultyId = model.FacultyId;
            t.UpdatedAt = DateTime.UtcNow;

            await _classRepo.UpsertAsync(x => x.Id == t.Id, t);

            TempData["Msg"] = "Đã cập nhật lớp.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/classes/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await CanManageAsync()) return Redirect("/auth/denied");

            var students = await _studentRepo.GetAllAsync();
            if (students.Any(u => (u.StudentClassId ?? "") == id))
            {
                TempData["Err"] = "Không thể xóa: còn sinh viên thuộc lớp này.";
                return RedirectToAction(nameof(Index));
            }

            await _classRepo.DeleteAsync(x => x.Id == id);
            TempData["Msg"] = "Đã xóa lớp.";
            return RedirectToAction(nameof(Index));
        }
    }
}

