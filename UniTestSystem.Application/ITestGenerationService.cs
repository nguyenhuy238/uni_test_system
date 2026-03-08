using UniTestSystem.Domain;

namespace UniTestSystem.Application
{
    public interface ITestGenerationService
    {
        // Generate 1 test cho 1 nhóm (giữ lại nếu bạn cần)
        Task<(Test test, List<TestItem> items, List<Student> targets)> GenerateAsync(
            AutoTestOptions opt, string actorUserName);

        // NEW: Generate CÁ NHÂN HOÁ — mỗi user 1 đề (DRAFT, chưa assign)
        Task<List<PersonalizedTestResult>> GeneratePersonalizedAsync(
            AutoTestOptions opt, string actorUserName);

        // Tuỳ chọn: generate + assign ngay (vẫn giữ nguyên nếu bạn đang dùng)
        Task<(Test test, List<TestItem> items, List<Student> assignedUsers)> GenerateAndAssignAsync(
            AutoTestOptions opt, string actorUserName);
    }

    public class PersonalizedTestResult
    {
        public Student User { get; set; } = new();
        public Test Test { get; set; } = new();
        public List<TestItem> Items { get; set; } = new();
    }
}
