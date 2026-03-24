using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public class ExamScheduleService : IExamScheduleService
    {
        private readonly IRepository<ExamSchedule> _scheduleRepo;
        private readonly IRepository<Enrollment> _enrollmentRepo;
        private readonly IRepository<Course> _courseRepo;
        private readonly IConfiguration _configuration;

        public ExamScheduleService(
            IRepository<ExamSchedule> scheduleRepo,
            IRepository<Enrollment> enrollmentRepo,
            IRepository<Course> courseRepo,
            IConfiguration configuration)
        {
            _scheduleRepo = scheduleRepo;
            _enrollmentRepo = enrollmentRepo;
            _courseRepo = courseRepo;
            _configuration = configuration;
        }

        public async Task<List<ExamSchedule>> GetAllSchedulesAsync()
        {
            return await _scheduleRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.Course)
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.StartTime)
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
            ValidateTimeRange(schedule.StartTime, schedule.EndTime);

            // Verify no room conflict
            if (await HasConflictAsync(schedule.Room, schedule.StartTime, schedule.EndTime))
            {
                throw new Exception("Room conflict detected for this time range.");
            }

            await ValidateBusinessConflictsAsync(schedule.CourseId, schedule.Room, schedule.StartTime, schedule.EndTime);

            schedule.Id = string.IsNullOrWhiteSpace(schedule.Id) ? Guid.NewGuid().ToString("N") : schedule.Id;
            schedule.CreatedAt = DateTime.UtcNow;
            await _scheduleRepo.InsertAsync(schedule);
            return true;
        }

        public async Task<bool> UpdateScheduleAsync(ExamSchedule schedule)
        {
            ValidateTimeRange(schedule.StartTime, schedule.EndTime);

            if (await HasConflictAsync(schedule.Room, schedule.StartTime, schedule.EndTime, schedule.Id))
            {
                throw new Exception("Room conflict detected for this time range.");
            }

            await ValidateBusinessConflictsAsync(
                schedule.CourseId,
                schedule.Room,
                schedule.StartTime,
                schedule.EndTime,
                schedule.Id);

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
                               IsOverlapping(start, end, s.StartTime, s.EndTime));
        }

        public async Task<List<string>> GetConflictingStudentsAsync(string courseId, DateTime start, DateTime end, string? excludeScheduleId = null)
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
                            x.s.Id != excludeScheduleId &&
                            IsOverlapping(start, end, x.s.StartTime, x.s.EndTime))
                .Select(x => x.e.StudentId)
                .Distinct()
                .ToListAsync();

            return conflictingStudents;
        }

        public async Task<List<string>> GetConflictingLecturersAsync(string courseId, DateTime start, DateTime end, string? excludeScheduleId = null)
        {
            var course = await _courseRepo.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null || string.IsNullOrWhiteSpace(course.LecturerId))
            {
                return new List<string>();
            }

            var lecturerId = course.LecturerId;

            var conflictingCourseIds = await _courseRepo.Query()
                .Where(c => !c.IsDeleted && c.LecturerId == lecturerId)
                .Select(c => c.Id)
                .ToListAsync();

            if (!conflictingCourseIds.Any())
            {
                return new List<string>();
            }

            var conflicted = await _scheduleRepo.Query()
                .Where(s => !s.IsDeleted
                            && s.Id != excludeScheduleId
                            && conflictingCourseIds.Contains(s.CourseId)
                            && IsOverlapping(start, end, s.StartTime, s.EndTime))
                .Select(s => s.CourseId)
                .Distinct()
                .ToListAsync();

            return conflicted;
        }

        public async Task<(bool IsOverCapacity, int EnrolledCount, int? Capacity)> CheckRoomCapacityAsync(string room, string courseId)
        {
            var capacity = ResolveRoomCapacity(room);

            var enrolledCount = await _enrollmentRepo.Query()
                .Where(e => !e.IsDeleted && e.CourseId == courseId)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            if (!capacity.HasValue)
            {
                return (false, enrolledCount, null);
            }

            return (enrolledCount > capacity.Value, enrolledCount, capacity.Value);
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

        private async Task ValidateBusinessConflictsAsync(
            string courseId,
            string room,
            DateTime start,
            DateTime end,
            string? excludeScheduleId = null)
        {
            var conflictingStudents = await GetConflictingStudentsAsync(courseId, start, end, excludeScheduleId);
            if (conflictingStudents.Count > 0)
            {
                var sample = string.Join(", ", conflictingStudents.Take(8));
                throw new Exception($"Student conflict detected ({conflictingStudents.Count} student(s)). Sample IDs: {sample}");
            }

            var conflictingLecturerCourses = await GetConflictingLecturersAsync(courseId, start, end, excludeScheduleId);
            if (conflictingLecturerCourses.Count > 0)
            {
                var sample = string.Join(", ", conflictingLecturerCourses.Take(5));
                throw new Exception($"Lecturer conflict detected with other scheduled course(s): {sample}");
            }

            var capacityResult = await CheckRoomCapacityAsync(room, courseId);
            if (capacityResult.IsOverCapacity)
            {
                throw new Exception(
                    $"Room capacity exceeded. Enrolled: {capacityResult.EnrolledCount}, Capacity: {capacityResult.Capacity}");
            }
        }

        private static void ValidateTimeRange(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new Exception("End time must be later than start time.");
            }
        }

        private int? ResolveRoomCapacity(string room)
        {
            if (string.IsNullOrWhiteSpace(room))
            {
                return null;
            }

            var section = _configuration.GetSection("ExamScheduling:RoomCapacities");
            foreach (var child in section.GetChildren())
            {
                if (string.Equals(child.Key, room, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(child.Value, out var cap)
                    && cap > 0)
                {
                    return cap;
                }
            }

            return null;
        }

        private static bool IsOverlapping(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        {
            return startA < endB && endA > startB;
        }
    }
}
