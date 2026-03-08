using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using ClosedXML.Excel;
using BCrypt.Net;

namespace UniTestSystem.Application;

public class BulkImportService : IBulkImportService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<StudentClass> _classRepo;

    public BulkImportService(
        IRepository<User> userRepo,
        IRepository<Student> studentRepo,
        IRepository<Course> courseRepo,
        IRepository<StudentClass> classRepo)
    {
        _userRepo = userRepo;
        _studentRepo = studentRepo;
        _courseRepo = courseRepo;
        _classRepo = classRepo;
    }

    public async Task<ImportResult> ImportStudentsAsync(Stream fileStream, string? defaultClassId = null)
    {
        var result = new ImportResult();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1); // Skip header

        foreach (var row in rows)
        {
            result.Total++;
            try
            {
                var name = row.Cell(1).Value.ToString().Trim();
                var email = row.Cell(2).Value.ToString().Trim();
                var code = row.Cell(3).Value.ToString().Trim();
                var major = row.Cell(4).Value.ToString().Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                {
                    result.Errors.Add($"Row {row.RowNumber()}: Name and Email are required.");
                    continue;
                }

                var existing = await _userRepo.FirstOrDefaultAsync(u => u.Email == email);
                if (existing != null)
                {
                    result.Skipped++;
                    result.SkippedReasons.Add($"Row {row.RowNumber()}: Email {email} already exists.");
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
                result.Errors.Add($"Row {row.RowNumber()}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ImportResult> ImportCoursesAsync(Stream fileStream)
    {
        var result = new ImportResult();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1); // Skip header

        foreach (var row in rows)
        {
            result.Total++;
            try
            {
                var name = row.Cell(1).Value.ToString().Trim();
                var code = row.Cell(2).Value.ToString().Trim();
                var creditsStr = row.Cell(3).Value.ToString().Trim();
                var area = row.Cell(4).Value.ToString().Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
                {
                    result.Errors.Add($"Row {row.RowNumber()}: Name and Code are required.");
                    continue;
                }

                var existing = await _courseRepo.FirstOrDefaultAsync(c => c.Code == code);
                if (existing != null)
                {
                    result.Skipped++;
                    result.SkippedReasons.Add($"Row {row.RowNumber()}: Course code {code} already exists.");
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
                result.Errors.Add($"Row {row.RowNumber()}: {ex.Message}");
            }
        }

        return result;
    }
}
