using System;

namespace UniTestSystem.Domain
{
    public class UserSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string UserId { get; set; } = "";
        public virtual User? User { get; set; }
        
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
