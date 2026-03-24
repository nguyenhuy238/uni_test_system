using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IExamScheduleService
{
    Task<List<ExamSchedule>> GetAllSchedulesAsync();
    Task<ExamSchedule?> GetScheduleByIdAsync(string id);
    Task<bool> CreateScheduleAsync(ExamSchedule schedule);
    Task<bool> UpdateScheduleAsync(ExamSchedule schedule);
    Task<bool> DeleteScheduleAsync(string id);
    
    // Conflict detection
    Task<bool> HasConflictAsync(string room, DateTime start, DateTime end, string? excludeId = null);
    Task<List<string>> GetConflictingStudentsAsync(string courseId, DateTime start, DateTime end);
    
    // Student specific
    Task<List<ExamSchedule>> GetSchedulesForStudentAsync(string studentId);
}
