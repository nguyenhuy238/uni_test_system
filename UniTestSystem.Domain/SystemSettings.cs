using System;

namespace UniTestSystem.Domain
{
    public class SystemSettings
    {
        public string Id { get; set; } = "settings"; // 1 record duy nhất
        public string SystemName { get; set; } = "UniTestSystem";
        public string? LogoUrl { get; set; } = null; // /uploads/logo/logo.png

        public string? CurrentSemester { get; set; } // e.g., "Học kỳ 1"
        public string? CurrentAcademicYear { get; set; } // e.g., "2025-2026"

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }
}
