using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public class AcademicService : IAcademicService
{
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<SystemSettings> _settingsRepo;
    private readonly IRepository<StudentClass> _classRepo;
    private readonly IRepository<Faculty> _facultyRepo;
    private readonly IRepository<Student> _studentRepo;

    public AcademicService(
        IRepository<Course> courseRepo,
        IRepository<Enrollment> enrollmentRepo,
        IRepository<SystemSettings> settingsRepo,
        IRepository<StudentClass> classRepo,
        IRepository<Faculty> facultyRepo,
        IRepository<Student> studentRepo)
    {
        _courseRepo = courseRepo;
        _enrollmentRepo = enrollmentRepo;
        _settingsRepo = settingsRepo;
        _classRepo = classRepo;
        _facultyRepo = facultyRepo;
        _studentRepo = studentRepo;
    }

    public async Task<List<Course>> GetAllCoursesAsync()
    {
        return await _courseRepo.GetAllAsync(x => !x.IsDeleted);
    }

    public async Task<Course?> GetCourseByIdAsync(string id)
    {
        var spec = new Specification<Course>(c => c.Id == id && !c.IsDeleted)
            .Include(c => c.Lecturer!);
        return await _courseRepo.FirstOrDefaultAsync(spec);
    }

    public async Task<bool> CreateCourseAsync(Course course)
    {
        course.Id = string.IsNullOrWhiteSpace(course.Id) ? Guid.NewGuid().ToString("N") : course.Id;
        course.CreatedAt = DateTime.UtcNow;
        await _courseRepo.InsertAsync(course);
        return true;
    }

    public async Task<bool> UpdateCourseAsync(string id, Course course)
    {
        course.Id = id;
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepo.UpsertAsync(x => x.Id == id, course);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(string id)
    {
        var course = await _courseRepo.FirstOrDefaultAsync(x => x.Id == id);
        if (course == null) return false;
        
        course.IsDeleted = true;
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepo.UpdateAsync(course);
        return true;
    }

    public async Task<List<Enrollment>> GetEnrollmentsByCourseAsync(string courseId)
    {
        var spec = new Specification<Enrollment>(e => e.CourseId == courseId && !e.IsDeleted)
            .Include(e => e.Student!);
        return await _enrollmentRepo.ListAsync(spec);
    }

    public async Task<bool> EnrollStudentAsync(string studentId, string courseId, string semester)
    {
        var exists = await _enrollmentRepo.FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId && !x.IsDeleted);
        if (exists != null) return true;

        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid().ToString("N"),
            StudentId = studentId,
            CourseId = courseId,
            Semester = semester,
            EnrolledAt = DateTime.UtcNow
        };

        await _enrollmentRepo.InsertAsync(enrollment);
        return true;
    }

    public async Task<bool> UnenrollStudentAsync(string studentId, string courseId)
    {
        var enrollment = await _enrollmentRepo.FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId && !x.IsDeleted);
        if (enrollment == null) return false;

        enrollment.IsDeleted = true;
        await _enrollmentRepo.UpdateAsync(enrollment);
        return true;
    }

    public Task<List<StudentClass>> GetAllClassesAsync()
    {
        return _classRepo.GetAllAsync(x => !x.IsDeleted);
    }

    public Task<StudentClass?> GetClassByIdAsync(string id)
    {
        return _classRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<bool> CreateClassAsync(StudentClass studentClass)
    {
        studentClass.Id = string.IsNullOrWhiteSpace(studentClass.Id) ? Guid.NewGuid().ToString("N") : studentClass.Id;
        studentClass.CreatedAt = DateTime.UtcNow;
        await _classRepo.InsertAsync(studentClass);
        return true;
    }

    public async Task<bool> UpdateClassAsync(string id, StudentClass studentClass)
    {
        var existing = await _classRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (existing == null) return false;

        studentClass.Id = id;
        studentClass.UpdatedAt = DateTime.UtcNow;
        studentClass.CreatedAt = existing.CreatedAt;
        await _classRepo.UpsertAsync(x => x.Id == id, studentClass);
        return true;
    }

    public async Task<bool> DeleteClassAsync(string id)
    {
        var existing = await _classRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (existing == null) return false;

        await _classRepo.DeleteAsync(x => x.Id == id);
        return true;
    }

    public Task<bool> HasStudentsInClassAsync(string classId)
    {
        return _studentRepo.AnyAsync(x => x.StudentClassId == classId);
    }

    public async Task<Dictionary<string, int>> GetClassCountsByFacultyAsync()
    {
        return (await _classRepo.GetAllAsync(x => !x.IsDeleted))
            .GroupBy(x => x.FacultyId ?? string.Empty)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
    }

    public Task<List<Faculty>> GetAllFacultiesAsync()
    {
        return _facultyRepo.GetAllAsync(x => !x.IsDeleted);
    }

    public Task<Faculty?> GetFacultyByIdAsync(string id)
    {
        return _facultyRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<bool> CreateFacultyAsync(Faculty faculty)
    {
        faculty.Id = string.IsNullOrWhiteSpace(faculty.Id) ? Guid.NewGuid().ToString("N") : faculty.Id;
        faculty.CreatedAt = DateTime.UtcNow;
        await _facultyRepo.InsertAsync(faculty);
        return true;
    }

    public async Task<bool> UpdateFacultyAsync(string id, Faculty faculty)
    {
        var existing = await _facultyRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (existing == null) return false;

        faculty.Id = id;
        faculty.UpdatedAt = DateTime.UtcNow;
        faculty.CreatedAt = existing.CreatedAt;
        await _facultyRepo.UpsertAsync(x => x.Id == id, faculty);
        return true;
    }

    public async Task<bool> DeleteFacultyAsync(string id)
    {
        var existing = await _facultyRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (existing == null) return false;

        await _facultyRepo.DeleteAsync(x => x.Id == id);
        return true;
    }

    public async Task<bool> SoftDeleteFacultyAsync(string id)
    {
        var existing = await _facultyRepo.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (existing == null) return false;

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTime.UtcNow;
        await _facultyRepo.UpdateAsync(existing);
        return true;
    }

    public Task<bool> HasClassesInFacultyAsync(string facultyId)
    {
        return _classRepo.AnyAsync(x => x.FacultyId == facultyId && !x.IsDeleted);
    }

    public async Task<SystemSettings> GetSystemSettingsAsync()
    {
        var settings = await _settingsRepo.FirstOrDefaultAsync(x => x.Id == "settings");
        if (settings == null)
        {
            settings = new SystemSettings { Id = "settings" };
            await _settingsRepo.InsertAsync(settings);
        }
        return settings;
    }

    public async Task<bool> UpdateAcademicSettingsAsync(string semester, string academicYear)
    {
        var settings = await GetSystemSettingsAsync();
        settings.CurrentSemester = semester;
        settings.CurrentAcademicYear = academicYear;
        settings.UpdatedAt = DateTime.UtcNow;
        await _settingsRepo.UpdateAsync(settings);
        return true;
    }
}
