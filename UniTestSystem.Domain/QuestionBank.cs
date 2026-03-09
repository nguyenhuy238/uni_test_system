namespace UniTestSystem.Domain
{
    public class QuestionBank
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string CourseId { get; set; } = "";
        public virtual Course? Course { get; set; }
        
        public string Name { get; set; } = "";
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; } = false;
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
