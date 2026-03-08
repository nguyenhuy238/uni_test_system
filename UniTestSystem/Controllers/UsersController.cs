using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Org_Manage)]
    public class UsersController : Controller
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Lecturer> _lecturerRepo;
        private readonly IRepository<StudentClass> _classRepo;
        private readonly IRepository<Faculty> _facultyRepo;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IRepository<User> userRepo,
            IRepository<Student> studentRepo,
            IRepository<Lecturer> lecturerRepo,
            IRepository<StudentClass> classRepo,
            IRepository<Faculty> facultyRepo,
            ILogger<UsersController> logger)
        {
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _lecturerRepo = lecturerRepo;
            _classRepo = classRepo;
            _facultyRepo = facultyRepo;
            _logger = logger;
        }

        // GET: /Users
        public async Task<IActionResult> Index(string? q, string? classId, Role? role)
        {
            var students = await _studentRepo.GetAllAsync();
            var lecturers = await _lecturerRepo.GetAllAsync();
            var admins = (await _userRepo.GetAllAsync()).Where(u => u.Role == Role.Admin).ToList();

            var allUsers = new List<User>();
            allUsers.AddRange(students);
            allUsers.AddRange(lecturers);
            allUsers.AddRange(admins);

            var filtered = allUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
                filtered = filtered.Where(u =>
                    (u.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u is Student s && (s.StudentCode?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)) ||
                    (u is Lecturer l && (l.LecturerCode?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)));

            if (!string.IsNullOrWhiteSpace(classId))
                filtered = filtered.Where(u => u is Student s && s.StudentClassId == classId);

            if (role.HasValue)
                filtered = filtered.Where(u => u.Role == role.Value);

            ViewBag.Classes = await _classRepo.GetAllAsync();
            ViewBag.Query = q;
            ViewBag.ClassId = classId;
            ViewBag.Role = role;

            return View(filtered.OrderBy(u => u.Name).ToList());
        }

        // GET: /Users/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View(new UserFormViewModel());
        }

        // POST: /Users/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel vm)
        {
            await LoadLookups();

            var all = await _userRepo.GetAllAsync();
            if (all.Any(u => u.Email.Equals(vm.Email, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại.");

            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Password là bắt buộc khi tạo mới.");

            if (!ModelState.IsValid) return View(vm);

            User entity;
            if (vm.Role == Role.Student)
            {
                entity = new Student
                {
                    StudentCode = vm.StudentCode?.Trim() ?? "",
                    StudentClassId = vm.StudentClassId,
                    AcademicYear = vm.AcademicYear.Trim() ?? "2024",
                    Major = vm.Major?.Trim() ?? ""
                };
            }
            else if (vm.Role == Role.Lecturer)
            {
                var faculty = await _facultyRepo.FirstOrDefaultAsync(f => f.Name == vm.FacultyName);
                entity = new Lecturer
                {
                    LecturerCode = vm.StudentCode?.Trim() ?? "", // Dùng chung field cho Code
                    FacultyId = faculty?.Id
                };
            }
            else
            {
                entity = new User();
            }

            entity.Id = Guid.NewGuid().ToString("N");
            entity.Name = vm.Name.Trim();
            entity.Email = vm.Email.Trim();
            entity.Role = vm.Role;
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password!);

            if (entity is Student s) await _studentRepo.InsertAsync(s);
            else if (entity is Lecturer l) await _lecturerRepo.InsertAsync(l);
            else await _userRepo.InsertAsync(entity);

            TempData["Msg"] = "Tạo người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            await LoadLookups();

            var vm = new UserFormViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role
            };

            if (u.Role == Role.Student)
            {
                var s = await _studentRepo.FirstOrDefaultAsync(x => x.Id == id);
                if (s != null)
                {
                    vm.AcademicYear = s.AcademicYear;
                    vm.StudentClassId = s.StudentClassId;
                    vm.Major = s.Major;
                    vm.StudentCode = s.StudentCode;
                }
            }
            else if (u.Role == Role.Lecturer)
            {
                var l = await _lecturerRepo.FirstOrDefaultAsync(x => x.Id == id);
                if (l != null)
                {
                    vm.FacultyName = l.Faculty?.Name;
                    vm.StudentCode = l.LecturerCode;
                }
            }

            return View(vm);
        }

        // POST: /Users/Edit/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserFormViewModel vm)
        {
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            await LoadLookups();

            var all = await _userRepo.GetAllAsync();
            if (all.Any(x => x.Email.Equals(vm.Email, StringComparison.OrdinalIgnoreCase) && x.Id != id))
                ModelState.AddModelError(nameof(vm.Email), "Email đã được dùng bởi người dùng khác.");

            if (!ModelState.IsValid) return View(vm);

            u.Name = vm.Name.Trim();
            u.Email = vm.Email.Trim();
            u.Role = vm.Role;

            if (u.Role == Role.Student)
            {
                var s = await _studentRepo.FirstOrDefaultAsync(x => x.Id == id);
                if (s != null)
                {
                    s.AcademicYear = vm.AcademicYear.Trim();
                    s.StudentClassId = vm.StudentClassId;
                    s.Major = vm.Major?.Trim() ?? "";
                    s.StudentCode = vm.StudentCode?.Trim() ?? "";
                    await _studentRepo.UpsertAsync(x => x.Id == id, s);
                }
            }
            else if (u.Role == Role.Lecturer)
            {
                var l = await _lecturerRepo.FirstOrDefaultAsync(x => x.Id == id);
                if (l != null)
                {
                    l.LecturerCode = vm.StudentCode?.Trim() ?? "";
                    var faculty = await _facultyRepo.FirstOrDefaultAsync(f => f.Name == vm.FacultyName);
                    l.FacultyId = faculty?.Id;
                    await _lecturerRepo.UpsertAsync(x => x.Id == id, l);
                }
            }
            else
            {
                await _userRepo.UpsertAsync(x => x.Id == id, u);
            }

            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);
                await _userRepo.UpsertAsync(x => x.Id == id, u);
            }

            TempData["Msg"] = "Cập nhật người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _userRepo.DeleteAsync(x => x.Id == id);
            TempData["Msg"] = "Đã xóa người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/ResetPassword/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Err"] = "Password mới không được trống.";
                return RedirectToAction(nameof(Index));
            }

            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.UpsertAsync(x => x.Id == id, u);
            TempData["Msg"] = "Đã reset mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLookups()
        {
            var classes = await _classRepo.GetAllAsync();
            ViewBag.ClassItems = new SelectList(classes, "Id", "Name");

            var faculties = await _facultyRepo.GetAllAsync();
            ViewBag.FacultyItems = new SelectList(faculties.Select(f => f.Name).Distinct());

            var years = new List<string> { "2021", "2022", "2023", "2024", "2025" };
            ViewBag.YearItems = new SelectList(years);
        }
    }
}
