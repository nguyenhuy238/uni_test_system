using System;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    /// <summary>
    /// Bản sao cố định của câu hỏi tại thời điểm đề thi được xuất bản hoặc sử dụng.
    /// Giúp tránh việc thay đổi nội dung câu hỏi gốc ảnh hưởng đến các đề thi đã chốt.
    /// </summary>
    public class TestQuestionSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        [Required]
        public string TestId { get; set; } = "";
        public virtual Test? Test { get; set; }

        [Required]
        public string OriginalQuestionId { get; set; } = "";
        
        [Required]
        public string Content { get; set; } = "";
        
        public QType Type { get; set; }
        
        /// <summary>
        /// Danh sách đáp án được Serialize JSON.
        /// </summary>
        public string? OptionsJson { get; set; } 

        /// <summary>
        /// Đáp án đúng được Serialize JSON.
        /// </summary>
        public string? CorrectAnswerJson { get; set; } 
        
        /// <summary>
        /// Điểm của câu hỏi này trong đề thi cụ thể.
        /// </summary>
        public decimal Points { get; set; } = 1m;

        /// <summary>
        /// Thứ tự hiển thị.
        /// </summary>
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
