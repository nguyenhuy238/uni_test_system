namespace UniTestSystem.Domain
{
    public class SessionLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = "";
        public virtual Session? Session { get; set; }
        
        public string ActionType { get; set; } = ""; // TabSwitch, Blur, Copy, Paste, etc.
        public string? Detail { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IPAddress { get; set; }
    }
}
