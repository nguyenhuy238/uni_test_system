using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IAcademicService
{
    Task<List<Course>> GetAllCoursesAsync();
    Task<Course?> GetCourseByIdAsync(string id);
    Task<bool> CreateCourseAsync(Course course);
    Task<bool> UpdateCourseAsync(string id, Course course);
    Task<bool> DeleteCourseAsync(string id);

    Task<List<Enrollment>> GetEnrollmentsByCourseAsync(string courseId);
    Task<bool> EnrollStudentAsync(string studentId, string courseId, string semester);
    Task<bool> UnenrollStudentAsync(string studentId, string courseId);

    Task<SystemSettings> GetSystemSettingsAsync();
    Task<bool> UpdateAcademicSettingsAsync(string semester, string academicYear);
}
