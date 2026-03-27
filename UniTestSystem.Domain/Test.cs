using System;

namespace UniTestSystem.Domain
{
    public class Test
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string? AssessmentId { get; set; }
        public virtual Assessment? Assessment { get; set; }

        public string Title { get; set; } = "";

        // Loại bài test: Test hoặc Test
        public TestType Type { get; set; } = TestType.Test;

        // Cấu hình chung
        public int DurationMinutes { get; set; } = 30;

        public AssessmentType AssessmentType { get; set; } = AssessmentType.Quiz;

        /// <summary>
        /// Ngưỡng đậu tính theo "điểm" (không phải số câu).
        /// Mặc định 5/10 (tương đương 50%).
        /// </summary>
        public int PassScore { get; set; } = 5;

        public bool ShuffleQuestions { get; set; } = true;

        /// <summary>
        /// Hoán vị các đáp án trong câu hỏi (nếu loại câu hỏi cho phép).
        /// </summary>
        public bool ShuffleOptions { get; set; } = true;

        // === Tổng điểm đề (mặc định 10.0) ===
        public decimal TotalMaxScore { get; set; } = 10m;

        public bool IsPublished { get; set; } = false;
        public bool IsArchived { get; set; } = false;

        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

        // === NEW: Many-to-Many Relational Structure ===
        public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

        /// <summary>
        /// Bản sao cố định của các câu hỏi khi đề thi được xuất bản.
        /// </summary>
        public virtual ICollection<TestQuestionSnapshot> QuestionSnapshots { get; set; } = new List<TestQuestionSnapshot>();

        // === NEW: Liên kết với Course ===
        public string? CourseId { get; set; }
        public virtual Course? Course { get; set; }

        // Audit
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Câu hỏi nằm trong Test cùng số điểm của nó.
    /// </summary>
    public class TestItem
    {
        public string QuestionId { get; set; } = "";
        public decimal Points { get; set; } = 1m;
    }
}
