using System;

namespace Employee_Survey.Domain
{
    public class SystemSettings
    {
        public string Id { get; set; } = "settings"; // 1 record duy nhất
        public string SystemName { get; set; } = "Employee Survey";
        public string? LogoUrl { get; set; } = null; // /uploads/logo/logo.png
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }
}
