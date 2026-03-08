namespace UniTestSystem.Domain
{
    public class Session
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string TestId { get; set; } = "";
        public virtual Test? Test { get; set; }
        public string UserId { get; set; } = "";
        public virtual User? User { get; set; }
        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndAt { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.InProgress;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // Timer (pause/resume)
        public int ConsumedSeconds { get; set; } = 0;
        public DateTime? TimerStartedAt { get; set; }  // ✅ chỉ set ở /resume

        // Concurrency
        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Scoring
        public decimal AutoScore { get; set; } = 0;
        public decimal ManualScore { get; set; } = 0;
        public decimal TotalScore { get; set; } = 0;
        public decimal MaxScore { get; set; } = 0;
        public decimal Percent { get; set; } = 0;
        public bool IsPassed { get; set; } = false;

        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
        public virtual ICollection<SessionLog> Logs { get; set; } = new List<SessionLog>();
        public virtual ICollection<DeviceFingerprint> DeviceFingerprints { get; set; } = new List<DeviceFingerprint>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        // Snapshot of questions (now using StudentAnswer for results, but might still need Snapshot if we want to freeze question content)
        // Decided to keep snapshot as JSON for "frozen" state if necessary, but the request implies normalization.
        // Actually, if we want to review/audit, we should probably have a normalized snapshot or versioned questions.
        // The plan says "Move answers from JSON blob to StudentAnswer table". 
        // I'll remove Snapshot JSON and rely on versioned questions or SessionQuestionSnapshots if they exist.
        // Wait, AppDbContext.cs had SessionQuestionSnapshots.

        // Audit & Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class SessionItem
    {
        public string QuestionId { get; set; } = "";
        public decimal Points { get; set; } = 1m;
    }
}
