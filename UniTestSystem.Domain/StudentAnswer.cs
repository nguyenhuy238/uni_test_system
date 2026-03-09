namespace UniTestSystem.Domain
{
    public class StudentAnswer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = "";
        public virtual Session? Session { get; set; }
        
        public string QuestionId { get; set; } = "";
        public virtual Question? Question { get; set; }
        
        public string? SelectedOptionId { get; set; }
        public string? EssayAnswer { get; set; }
        public decimal Score { get; set; } = 0;
        
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
