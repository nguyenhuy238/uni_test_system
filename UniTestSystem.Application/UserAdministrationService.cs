using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Lecturer> _lecturerRepo;
    private readonly IRepository<StudentClass> _classRepo;
    private readonly IRepository<Faculty> _facultyRepo;
    private readonly AuthService _authService;

    public UserAdministrationService(
        IRepository<User> userRepo,
        IRepository<Student> studentRepo,
        IRepository<Lecturer> lecturerRepo,
        IRepository<StudentClass> classRepo,
        IRepository<Faculty> facultyRepo,
        AuthService authService)
    {
        _userRepo = userRepo;
        _studentRepo = studentRepo;
        _lecturerRepo = lecturerRepo;
        _classRepo = classRepo;
        _facultyRepo = facultyRepo;
        _authService = authService;
    }

    public async Task<List<User>> SearchAsync(string? query, string? classId, Role? role)
    {
        var students = await _studentRepo.GetAllAsync();
        var lecturers = await _lecturerRepo.GetAllAsync();
        var admins = (await _userRepo.GetAllAsync()).Where(u => u.Role == Role.Admin).ToList();

        var allUsers = new List<User>();
        allUsers.AddRange(students);
        allUsers.AddRange(lecturers);
        allUsers.AddRange(admins);

        var filtered = allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(u =>
                (u.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u is Student s && (s.StudentCode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)) ||
                (u is Lecturer l && (l.LecturerCode?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)));
        }

        if (!string.IsNullOrWhiteSpace(classId))
        {
            filtered = filtered.Where(u => u is Student s && s.StudentClassId == classId);
        }

        if (role.HasValue)
        {
            filtered = filtered.Where(u => u.Role == role.Value);
        }

        return filtered.OrderBy(u => u.Name).ToList();
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return _userRepo.GetAllAsync();
    }

    public Task<User?> GetUserByIdAsync(string id)
    {
        return _userRepo.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Student?> GetStudentByIdAsync(string id)
    {
        return _studentRepo.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<Lecturer?> GetLecturerByIdAsync(string id)
    {
        return _lecturerRepo.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
    {
        var normalized = email.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return false;

        var all = await _userRepo.GetAllAsync();
        return all.Any(x =>
            x.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase) &&
            x.Id != (excludeUserId ?? string.Empty));
    }

    public async Task<bool> AssignRoleAsync(string userId, Role role)
    {
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return false;

        user.Role = role;
        await _userRepo.UpsertAsync(x => x.Id == user.Id, user);
        return true;
    }

    public async Task<bool> CreateRawAsync(User user)
    {
        user.Id = string.IsNullOrWhiteSpace(user.Id) ? Guid.NewGuid().ToString("N") : user.Id;
        await _userRepo.InsertAsync(user);
        return true;
    }

    public async Task<bool> UpdateRawAsync(string id, User user)
    {
        var existing = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return false;

        user.Id = id;
        await _userRepo.UpsertAsync(x => x.Id == id, user);
        return true;
    }

    public async Task<bool> DeleteRawAsync(string id)
    {
        var existing = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return false;

        await _userRepo.DeleteAsync(x => x.Id == id);
        return true;
    }

    public Task UpsertUserAsync(User user)
    {
        return _userRepo.UpsertAsync(x => x.Id == user.Id, user);
    }

    public Task UpsertStudentAsync(Student student)
    {
        return _studentRepo.UpsertAsync(x => x.Id == student.Id, student);
    }

    public Task UpsertLecturerAsync(Lecturer lecturer)
    {
        return _lecturerRepo.UpsertAsync(x => x.Id == lecturer.Id, lecturer);
    }

    public async Task<UserLookupData> GetLookupDataAsync()
    {
        var classes = await _classRepo.GetAllAsync();
        var faculties = await _facultyRepo.GetAllAsync();

        return new UserLookupData
        {
            Classes = classes,
            FacultyNames = faculties
                .Select(f => f.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList()!,
            AcademicYears = new List<string> { "2021", "2022", "2023", "2024", "2025" }
        };
    }

    public async Task<UserFormData?> GetUserFormAsync(string id)
    {
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return null;
        }

        var form = new UserFormData
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };

        if (user.Role == Role.Student)
        {
            var student = await _studentRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (student != null)
            {
                form.AcademicYear = student.AcademicYear;
                form.StudentClassId = student.StudentClassId;
                form.Major = student.Major;
                form.StudentCode = student.StudentCode;
            }
        }
        else if (user.Role == Role.Lecturer)
        {
            var lecturer = await _lecturerRepo.FirstOrDefaultAsync(x => x.Id == id);
            if (lecturer != null)
            {
                form.StudentCode = lecturer.LecturerCode;
                if (!string.IsNullOrWhiteSpace(lecturer.FacultyId))
                {
                    var faculty = await _facultyRepo.FirstOrDefaultAsync(f => f.Id == lecturer.FacultyId);
                    form.FacultyName = faculty?.Name;
                }
            }
        }

        return form;
    }

    public async Task<(bool Success, string? Error)> CreateAsync(UserUpsertCommand command)
    {
        var email = command.Email.Trim();
        var all = await _userRepo.GetAllAsync();
        if (all.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Email đã tồn tại.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return (false, "Password là bắt buộc khi tạo mới.");
        }

        User entity;
        if (command.Role == Role.Student)
        {
            entity = new Student
            {
                StudentCode = command.StudentCode?.Trim() ?? "",
                StudentClassId = command.StudentClassId,
                AcademicYear = command.AcademicYear.Trim(),
                Major = command.Major?.Trim() ?? ""
            };
        }
        else if (command.Role == Role.Lecturer)
        {
            var faculty = await _facultyRepo.FirstOrDefaultAsync(f => f.Name == command.FacultyName);
            entity = new Lecturer
            {
                LecturerCode = command.StudentCode?.Trim() ?? "",
                FacultyId = faculty?.Id
            };
        }
        else
        {
            entity = new User();
        }

        entity.Id = Guid.NewGuid().ToString("N");
        entity.Name = command.Name.Trim();
        entity.Email = email;
        entity.Role = command.Role;
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

        if (entity is Student student)
        {
            await _studentRepo.InsertAsync(student);
        }
        else if (entity is Lecturer lecturer)
        {
            await _lecturerRepo.InsertAsync(lecturer);
        }
        else
        {
            await _userRepo.InsertAsync(entity);
        }

        return (true, null);
    }

    public async Task<UserUpdateResult> UpdateAsync(string id, UserUpsertCommand command, string revokedByIp)
    {
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return new UserUpdateResult { Found = false, Success = false, Error = "Không tìm thấy người dùng." };
        }

        var all = await _userRepo.GetAllAsync();
        if (all.Any(x => x.Email.Equals(command.Email.Trim(), StringComparison.OrdinalIgnoreCase) && x.Id != id))
        {
            return new UserUpdateResult { Found = true, Success = false, Error = "Email đã được dùng bởi người dùng khác." };
        }

        var oldRole = user.Role;

        user.Name = command.Name.Trim();
        user.Email = command.Email.Trim();
        user.Role = command.Role;
        await _userRepo.UpsertAsync(x => x.Id == id, user);

        if (user.Role == Role.Student)
        {
            var student = await _studentRepo.FirstOrDefaultAsync(x => x.Id == id) ?? new Student { Id = id };
            student.Name = user.Name;
            student.Email = user.Email;
            student.Role = user.Role;
            student.PasswordHash = user.PasswordHash;
            student.AcademicYear = command.AcademicYear.Trim();
            student.StudentClassId = command.StudentClassId;
            student.Major = command.Major?.Trim() ?? "";
            student.StudentCode = command.StudentCode?.Trim() ?? "";
            await _studentRepo.UpsertAsync(x => x.Id == id, student);
        }
        else if (user.Role == Role.Lecturer)
        {
            var lecturer = await _lecturerRepo.FirstOrDefaultAsync(x => x.Id == id) ?? new Lecturer { Id = id };
            lecturer.Name = user.Name;
            lecturer.Email = user.Email;
            lecturer.Role = user.Role;
            lecturer.PasswordHash = user.PasswordHash;
            lecturer.LecturerCode = command.StudentCode?.Trim() ?? "";
            var faculty = await _facultyRepo.FirstOrDefaultAsync(f => f.Name == command.FacultyName);
            lecturer.FacultyId = faculty?.Id;
            await _lecturerRepo.UpsertAsync(x => x.Id == id, lecturer);
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);
            await _userRepo.UpsertAsync(x => x.Id == id, user);
        }

        var roleChanged = oldRole != user.Role;
        if (roleChanged)
        {
            await _authService.InvalidateAllAuthSessionsAsync(user.Id, revokedByIp);
        }

        return new UserUpdateResult
        {
            Found = true,
            Success = true,
            RoleChanged = roleChanged,
            Message = roleChanged
                ? $"Cập nhật người dùng thành công. Đã vô hiệu hóa mọi phiên đăng nhập do thay đổi vai trò ({oldRole} -> {user.Role})."
                : "Cập nhật người dùng thành công."
        };
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(string id)
    {
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        await _userRepo.DeleteAsync(x => x.Id == id);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "Password mới không được trống.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userRepo.UpsertAsync(x => x.Id == id, user);
        return (true, null);
    }
}
