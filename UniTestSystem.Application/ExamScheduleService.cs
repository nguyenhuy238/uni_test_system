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
            var spec = new Specification<ExamSchedule>(s => !s.IsDeleted)
                .Include(s => s.Test!)
                .Include(s => s.Course!);
            var schedules = await _scheduleRepo.ListAsync(spec);
            return schedules.OrderBy(s => s.StartTime).ToList();
        }

        public async Task<ExamSchedule?> GetScheduleByIdAsync(string id)
        {
            var spec = new Specification<ExamSchedule>(s => s.Id == id && !s.IsDeleted)
                .Include(s => s.Test!)
                .Include(s => s.Course!);
            return await _scheduleRepo.FirstOrDefaultAsync(spec);
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
            return await _scheduleRepo.AnyAsync(s =>
                !s.IsDeleted &&
                s.Room == room &&
                s.Id != excludeId &&
                s.StartTime < end &&
                s.EndTime > start);
        }

        public async Task<List<string>> GetConflictingStudentsAsync(string courseId, DateTime start, DateTime end, string? excludeScheduleId = null)
        {
            var studentIds = (await _enrollmentRepo.GetAllAsync(e => e.CourseId == courseId && !e.IsDeleted))
                .Select(e => e.StudentId)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            if (!studentIds.Any()) return new List<string>();

            var candidateEnrollments = await _enrollmentRepo.GetAllAsync(e =>
                !e.IsDeleted &&
                studentIds.Contains(e.StudentId) &&
                e.CourseId != courseId);

            var candidateCourseIds = candidateEnrollments
                .Select(e => e.CourseId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (!candidateCourseIds.Any())
            {
                return new List<string>();
            }

            var conflictingSchedules = await _scheduleRepo.GetAllAsync(s =>
                !s.IsDeleted &&
                s.Id != excludeScheduleId &&
                candidateCourseIds.Contains(s.CourseId) &&
                s.StartTime < end &&
                s.EndTime > start);

            var conflictingCourseIds = conflictingSchedules
                .Select(s => s.CourseId)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            return candidateEnrollments
                .Where(e => conflictingCourseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        public async Task<List<string>> GetConflictingLecturersAsync(string courseId, DateTime start, DateTime end, string? excludeScheduleId = null)
        {
            var course = await _courseRepo.FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null || string.IsNullOrWhiteSpace(course.LecturerId))
            {
                return new List<string>();
            }

            var lecturerId = course.LecturerId;

            var conflictingCourseIds = (await _courseRepo.GetAllAsync(c => !c.IsDeleted && c.LecturerId == lecturerId))
                .Select(c => c.Id)
                .ToList();

            if (!conflictingCourseIds.Any())
            {
                return new List<string>();
            }

            var conflicted = (await _scheduleRepo.GetAllAsync(s =>
                !s.IsDeleted
                && s.Id != excludeScheduleId
                && conflictingCourseIds.Contains(s.CourseId)
                && s.StartTime < end
                && s.EndTime > start))
                .Select(s => s.CourseId)
                .Distinct()
                .ToList();

            return conflicted;
        }

        public async Task<(bool IsOverCapacity, int EnrolledCount, int? Capacity)> CheckRoomCapacityAsync(string room, string courseId)
        {
            var capacity = ResolveRoomCapacity(room);

            var enrolledCount = (await _enrollmentRepo.GetAllAsync(e => !e.IsDeleted && e.CourseId == courseId))
                .Select(e => e.StudentId)
                .Distinct(StringComparer.Ordinal)
                .Count();

            if (!capacity.HasValue)
            {
                return (false, enrolledCount, null);
            }

            return (enrolledCount > capacity.Value, enrolledCount, capacity.Value);
        }

        public async Task<List<ExamSchedule>> GetSchedulesForStudentAsync(string studentId)
        {
            var courseIds = (await _enrollmentRepo.GetAllAsync(e => e.StudentId == studentId && !e.IsDeleted))
                .Select(e => e.CourseId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (!courseIds.Any())
            {
                return new List<ExamSchedule>();
            }

            var spec = new Specification<ExamSchedule>(s => !s.IsDeleted && courseIds.Contains(s.CourseId))
                .Include(s => s.Test!)
                .Include(s => s.Course!);

            var schedules = await _scheduleRepo.ListAsync(spec);
            return schedules.OrderBy(s => s.StartTime).ToList();
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
