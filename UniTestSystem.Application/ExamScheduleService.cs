using Microsoft.EntityFrameworkCore;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public class ExamScheduleService : IExamScheduleService
    {
        private readonly IRepository<ExamSchedule> _scheduleRepo;
        private readonly IRepository<Enrollment> _enrollmentRepo;

        public ExamScheduleService(
            IRepository<ExamSchedule> scheduleRepo,
            IRepository<Enrollment> enrollmentRepo)
        {
            _scheduleRepo = scheduleRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        public async Task<List<ExamSchedule>> GetAllSchedulesAsync()
        {
            return await _scheduleRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.Course)
                .Where(s => !s.IsDeleted)
                .ToListAsync();
        }

        public async Task<ExamSchedule?> GetScheduleByIdAsync(string id)
        {
            return await _scheduleRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }

        public async Task<bool> CreateScheduleAsync(ExamSchedule schedule)
        {
            // Verify no room conflict
            if (await HasConflictAsync(schedule.Room, schedule.StartTime, schedule.EndTime))
            {
                throw new Exception("Room conflict detected for this time range.");
            }

            schedule.Id = string.IsNullOrWhiteSpace(schedule.Id) ? Guid.NewGuid().ToString("N") : schedule.Id;
            schedule.CreatedAt = DateTime.UtcNow;
            await _scheduleRepo.InsertAsync(schedule);
            return true;
        }

        public async Task<bool> UpdateScheduleAsync(ExamSchedule schedule)
        {
            if (await HasConflictAsync(schedule.Room, schedule.StartTime, schedule.EndTime, schedule.Id))
            {
                throw new Exception("Room conflict detected for this time range.");
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _scheduleRepo.UpdateAsync(schedule);
            return true;
        }

        public async Task<bool> DeleteScheduleAsync(string id)
        {
            var schedule = await _scheduleRepo.FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null) return false;

            schedule.IsDeleted = true;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _scheduleRepo.UpdateAsync(schedule);
            return true;
        }

        public async Task<bool> HasConflictAsync(string room, DateTime start, DateTime end, string? excludeId = null)
        {
            return await _scheduleRepo.Query()
                .AnyAsync(s => !s.IsDeleted && 
                               s.Room == room && 
                               s.Id != excludeId &&
                               ((start >= s.StartTime && start < s.EndTime) || 
                                (end > s.StartTime && end <= s.EndTime) ||
                                (start <= s.StartTime && end >= s.EndTime)));
        }

        public async Task<List<string>> GetConflictingStudentsAsync(string courseId, DateTime start, DateTime end)
        {
            // Get all students enrolled in the current course
            var studentIds = await _enrollmentRepo.Query()
                .Where(e => e.CourseId == courseId && !e.IsDeleted)
                .Select(e => e.StudentId)
                .ToListAsync();

            if (!studentIds.Any()) return new List<string>();

            // Find if any of these students have another exam schedule at the same time
            var conflictingStudents = await _enrollmentRepo.Query()
                .Include(e => e.Course)
                .Join(_scheduleRepo.Query(),
                    e => e.CourseId,
                    s => s.CourseId,
                    (e, s) => new { e, s })
                .Where(x => !x.s.IsDeleted &&
                            studentIds.Contains(x.e.StudentId) &&
                            x.e.CourseId != courseId && // Different course
                            ((start >= x.s.StartTime && start < x.s.EndTime) || 
                             (end > x.s.StartTime && end <= x.s.EndTime) ||
                             (start <= x.s.StartTime && end >= x.s.EndTime)))
                .Select(x => x.e.StudentId)
                .Distinct()
                .ToListAsync();

            return conflictingStudents;
        }

        public async Task<List<ExamSchedule>> GetSchedulesForStudentAsync(string studentId)
        {
            var courseIds = await _enrollmentRepo.Query()
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _scheduleRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.Course)
                .Where(s => !s.IsDeleted && courseIds.Contains(s.CourseId))
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }
    }
}
