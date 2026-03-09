// UniTestSystem.Domain/Feedback.cs
namespace UniTestSystem.Domain
{
    public class Feedback
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = "";
        public virtual Session? Session { get; set; }
        public string Content { get; set; } = "";
        public int Rating { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? UpdatedAt { get; set; }
    }
}
