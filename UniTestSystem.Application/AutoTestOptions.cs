using System;
using System.Collections.Generic;

namespace UniTestSystem.Application
{
    /// <summary>
    /// Tham số cho Auto Generate (khớp View AutoTests/Generate.cshtml).
    /// </summary>
    public class AutoTestOptions
    {
        // Target
        public string Mode { get; set; } = "Faculty"; // "Faculty" | "Class" | "Students"
        public string? FacultyName { get; set; }
        public string? StudentClassId { get; set; }
        public List<string>? UserIds { get; set; }

        // Filters
        public List<string>? Subjects { get; set; }
        public string DifficultyPolicy { get; set; } = "ByYear"; // "ByYear" | "Any"

        // Per-type counts
        public int McqCount { get; set; } = 8;
        public int TfCount { get; set; } = 2;
        public int MatchingCount { get; set; } = 0;
        public int DragDropCount { get; set; } = 0;
        public int EssayCount { get; set; } = 0;

        // Scoring
        public decimal TotalScore { get; set; } = 10m;
        /// <summary>Điểm dành riêng cho Essay (0 => allocator tự mặc định 2.0 khi có Essay).</summary>
        public decimal EssayReserved { get; set; } = 0m;

        // Chính sách khi thiếu câu
        public bool FailWhenInsufficient { get; set; } = true;

        // Assign window (dùng cho Assign All)
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
    }
}
