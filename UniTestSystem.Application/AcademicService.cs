using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Application;

public class AcademicService : IAcademicService
{
    private readonly IRepository<Course> _courseRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<SystemSettings> _settingsRepo;

    public AcademicService(
        IRepository<Course> courseRepo,
        IRepository<Enrollment> enrollmentRepo,
        IRepository<SystemSettings> settingsRepo)
    {
        _courseRepo = courseRepo;
        _enrollmentRepo = enrollmentRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<List<Course>> GetAllCoursesAsync()
    {
        return await _courseRepo.GetAllAsync(x => !x.IsDeleted);
    }

    public async Task<Course?> GetCourseByIdAsync(string id)
    {
        return await _courseRepo.Query()
            .Include(c => c.Lecturer)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
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
        return await _enrollmentRepo.Query()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId && !e.IsDeleted)
            .ToListAsync();
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
