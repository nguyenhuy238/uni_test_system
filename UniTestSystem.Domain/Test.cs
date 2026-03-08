using System;

namespace UniTestSystem.Domain
{
    public class Test
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string AssessmentId { get; set; } = "";
        public virtual Assessment? Assessment { get; set; }

        public string Title { get; set; } = "";

        // Loại bài test: Test hoặc Test
        public TestType Type { get; set; } = TestType.Test;

        // Cấu hình chung
        public int DurationMinutes { get; set; } = 30;

        /// <summary>
        /// Ngưỡng đậu tính theo "điểm" (không phải số câu).
        /// Mặc định 5/10 (tương đương 50%).
        /// </summary>
        public int PassScore { get; set; } = 5;

        public bool ShuffleQuestions { get; set; } = true;

        // Cấu hình random cũ (giữ nguyên để tương thích)
        public string SubjectIdFilter { get; set; } = "Programming";
        public int RandomMCQ { get; set; } = 5;
        public int RandomTF { get; set; } = 5;
        public int RandomEssay { get; set; } = 0;

        // === Tổng điểm đề (mặc định 10.0) ===
        public decimal TotalMaxScore { get; set; } = 10m;

        public bool IsPublished { get; set; } = false;

        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

        // === NEW: Many-to-Many Relational Structure ===
        public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

        public FrozenRandomConfig? FrozenRandom { get; set; }

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

    public class FrozenRandomConfig
    {
        public string SubjectIdFilter { get; set; } = "Programming";
        public int RandomMCQ { get; set; }
        public int RandomTF { get; set; }
        public int RandomEssay { get; set; }
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
