using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IUserAdministrationService
{
    Task<List<User>> SearchAsync(string? query, string? classId, Role? role);
    Task<UserLookupData> GetLookupDataAsync();
    Task<UserFormData?> GetUserFormAsync(string id);
    Task<(bool Success, string? Error)> CreateAsync(UserUpsertCommand command);
    Task<UserUpdateResult> UpdateAsync(string id, UserUpsertCommand command, string revokedByIp);
    Task<(bool Success, string? Error)> DeleteAsync(string id);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string id, string newPassword);
}

public sealed class UserLookupData
{
    public List<StudentClass> Classes { get; set; } = new();
    public List<string> FacultyNames { get; set; } = new();
    public List<string> AcademicYears { get; set; } = new();
}

public sealed class UserFormData
{
    public string? Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Role Role { get; set; } = Role.Student;
    public string AcademicYear { get; set; } = "2024";
    public string? StudentClassId { get; set; }
    public string? FacultyName { get; set; }
    public string? Major { get; set; }
    public string? StudentCode { get; set; }
}

public sealed class UserUpsertCommand
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Role Role { get; set; } = Role.Student;
    public string AcademicYear { get; set; } = "2024";
    public string? StudentClassId { get; set; }
    public string? FacultyName { get; set; }
    public string? Major { get; set; }
    public string? StudentCode { get; set; }
    public string? Password { get; set; }
}

public sealed class UserUpdateResult
{
    public bool Found { get; set; }
    public bool Success { get; set; }
    public bool RoleChanged { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
