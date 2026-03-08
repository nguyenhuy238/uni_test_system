using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class SessionQuestionSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string SessionId { get; set; } = "";
        public virtual Session? Session { get; set; }

        public string OriginalQuestionId { get; set; } = "";
        
        public string Content { get; set; } = "";
        public QType Type { get; set; }
        public string? OptionsJson { get; set; } // Serialized options
        public string? CorrectAnswerJson { get; set; } // Serialized correct answers
        
        public decimal Points { get; set; } = 1m;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
