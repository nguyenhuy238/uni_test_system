using System;

namespace UniTestSystem.Domain
{
    public class RefreshToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Token { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByIp { get; set; }
        
        public string UserId { get; set; } = "";
        public virtual User? User { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
