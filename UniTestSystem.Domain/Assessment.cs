namespace UniTestSystem.Domain
{
    public class Assessment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CourseId { get; set; } = "";
        public virtual Course? Course { get; set; }
        
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public AssessmentType Type { get; set; } = AssessmentType.Assignment;
        public decimal Weight { get; set; } = 0; // % of final grade
        
        public string TargetType { get; set; } = "Course"; // Student, Class, Course
        public string? TargetValue { get; set; } // Id tương ứng
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
