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

    Task<List<StudentClass>> GetAllClassesAsync();
    Task<StudentClass?> GetClassByIdAsync(string id);
    Task<bool> CreateClassAsync(StudentClass studentClass);
    Task<bool> UpdateClassAsync(string id, StudentClass studentClass);
    Task<bool> DeleteClassAsync(string id);
    Task<bool> HasStudentsInClassAsync(string classId);
    Task<Dictionary<string, int>> GetClassCountsByFacultyAsync();

    Task<List<Faculty>> GetAllFacultiesAsync();
    Task<Faculty?> GetFacultyByIdAsync(string id);
    Task<bool> CreateFacultyAsync(Faculty faculty);
    Task<bool> UpdateFacultyAsync(string id, Faculty faculty);
    Task<bool> DeleteFacultyAsync(string id);
    Task<bool> SoftDeleteFacultyAsync(string id);
    Task<bool> HasClassesInFacultyAsync(string facultyId);

    Task<SystemSettings> GetSystemSettingsAsync();
    Task<bool> UpdateAcademicSettingsAsync(string semester, string academicYear);
}
