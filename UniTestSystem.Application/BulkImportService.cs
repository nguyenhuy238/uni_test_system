using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using BCrypt.Net;

namespace UniTestSystem.Application;

public class BulkImportService : IBulkImportService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<StudentClass> _classRepo;
    private readonly IBulkImportSpreadsheetReader _spreadsheetReader;

    public BulkImportService(
        IRepository<User> userRepo,
        IRepository<Student> studentRepo,
        IRepository<Course> courseRepo,
        IRepository<StudentClass> classRepo,
        IBulkImportSpreadsheetReader spreadsheetReader)
    {
        _userRepo = userRepo;
        _studentRepo = studentRepo;
        _courseRepo = courseRepo;
        _classRepo = classRepo;
        _spreadsheetReader = spreadsheetReader;
    }

    public async Task<ImportResult> ImportStudentsAsync(Stream fileStream, string? defaultClassId = null)
    {
        var result = new ImportResult();
        var rows = _spreadsheetReader.ReadStudents(fileStream);

        foreach (var row in rows)
        {
            result.Total++;
            try
            {
                var name = row.Name;
                var email = row.Email;
                var code = row.StudentCode;
                var major = row.Major;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                {
                    result.Errors.Add($"Row {row.RowNumber}: Name and Email are required.");
                    continue;
                }

                var existing = await _userRepo.FirstOrDefaultAsync(u => u.Email == email);
                if (existing != null)
                {
                    result.Skipped++;
                    result.SkippedReasons.Add($"Row {row.RowNumber}: Email {email} already exists.");
                    continue;
                }

                var student = new Student
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!"),
                    Role = Role.Student,
                    StudentCode = code,
                    Major = major,
                    StudentClassId = defaultClassId,
                    CreatedAt = DateTime.UtcNow
                };

                await _studentRepo.InsertAsync(student);
                result.Success++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row.RowNumber}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ImportResult> ImportCoursesAsync(Stream fileStream)
    {
        var result = new ImportResult();
        var rows = _spreadsheetReader.ReadCourses(fileStream);

        foreach (var row in rows)
        {
            result.Total++;
            try
            {
                var name = row.Name;
                var code = row.Code;
                var creditsStr = row.CreditsText;
                var area = row.SubjectArea;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                {
                    result.Errors.Add($"Row {row.RowNumber}: Name and Code are required.");
                    continue;
                }

                var existing = await _courseRepo.FirstOrDefaultAsync(c => c.Code == code);
                if (existing != null)
                {
                    result.Skipped++;
                    result.SkippedReasons.Add($"Row {row.RowNumber}: Course code {code} already exists.");
                    continue;
                }

                int.TryParse(creditsStr, out int credits);

                var course = new Course
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    Code = code,
                    Credits = credits > 0 ? credits : 3,
                    SubjectArea = area,
                    CreatedAt = DateTime.UtcNow
                };

                await _courseRepo.InsertAsync(course);
                result.Success++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row.RowNumber}: {ex.Message}");
            }
        }

        return result;
    }
}
