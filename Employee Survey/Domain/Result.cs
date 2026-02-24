namespace Employee_Survey.Domain
{
    public class Result
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = "";
        public string TestId { get; set; } = "";
        public decimal Score { get; set; } = 0;
        public decimal MaxScore { get; set; } = 10;
        public DateTime SubmitTime { get; set; } = DateTime.UtcNow;
        public SessionStatus Status { get; set; } = SessionStatus.Submitted;
        public string SessionId { get; set; } = ""; // Link to Session
    }
}
