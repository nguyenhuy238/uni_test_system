namespace UniTestSystem.Domain
{
    public class Result
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = "";
        public virtual User User { get; set; } = null!;

        public string TestId { get; set; } = "";
        public virtual Test Test { get; set; } = null!;

        public decimal Score { get; set; } = 0;
        public decimal MaxScore { get; set; } = 10;
        public DateTime SubmitTime { get; set; } = DateTime.UtcNow;
        public SessionStatus Status { get; set; } = SessionStatus.Submitted;

        public string SessionId { get; set; } = ""; // Link to Session
        public virtual Session Session { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
