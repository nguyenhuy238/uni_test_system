using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Application
{
    public class AssessmentService
    {
        private readonly IRepository<Assessment> _asRepo;
        private readonly IRepository<Student> _sRepo;
        private readonly IRepository<Test> _testRepo;
        private readonly IRepository<Enrollment> _enrollRepo;

        public AssessmentService(IRepository<Assessment> a, IRepository<Student> s, IRepository<Test> t, IRepository<Enrollment> e) 
        { 
            _asRepo = a; 
            _sRepo = s; 
            _testRepo = t;
            _enrollRepo = e;
        }

        // Lấy các TestId hợp lệ cho student
        public async Task<List<string>> GetAvailableTestIdsAsync(string userId, DateTime nowUtc)
        {
            var student = await _sRepo.FirstOrDefaultAsync(x => x.Id == userId) ?? throw new Exception("Student not found");
            var assessments = await _asRepo.GetAllAsync();

            // Lấy các course mà student đã enroll
            var enrollments = await _enrollRepo.GetAllAsync();
            var enrolledCourseIds = enrollments.Where(e => e.StudentId == userId).Select(e => e.CourseId).ToHashSet();

            // Lọc assessments hợp lệ
            var availableTests = await _testRepo.GetAllAsync();
            var publishedTestIds = availableTests.Where(t => t.IsPublished).Select(t => t.Id).ToHashSet();

            var resultIds = assessments.Where(a => a.StartTime <= nowUtc && nowUtc <= a.EndTime &&
                 (enrolledCourseIds.Contains(a.CourseId)))
                .Join(availableTests, a => a.Id, t => t.AssessmentId, (a, t) => t.Id)
                .Where(id => publishedTestIds.Contains(id))
                .Distinct()
                .ToList();

            return resultIds;
        }

        public async Task<List<Test>> GetAvailableTestsAsync(string userId, DateTime nowUtc)
        {
            var testIds = await GetAvailableTestIdsAsync(userId, nowUtc);
            var allTests = await _testRepo.GetAllAsync();
            return allTests.Where(t => testIds.Contains(t.Id)).ToList();
        }
    }
}
