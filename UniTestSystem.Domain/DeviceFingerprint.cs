namespace UniTestSystem.Domain
{
    public class DeviceFingerprint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = "";
        public virtual Session? Session { get; set; }
        
        public string Browser { get; set; } = "";
        public string OS { get; set; } = "";
        public string IP { get; set; } = "";
        public string UserAgent { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
