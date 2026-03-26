using System;

namespace UniTestSystem.Domain
{
    public class Transcript
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string StudentId { get; set; } = "";
        public virtual User? Student { get; set; }

        public decimal GPA { get; set; } = 0;
        public int TotalCredits { get; set; } = 0;
        public string? AcademicYear { get; set; }
        public decimal? YearEndGpa4 { get; set; }
        public decimal? YearEndGpa10 { get; set; }
        public int? YearEndTotalCreditsEarned { get; set; }
        public string? AcademicStatus { get; set; }
        public bool IsYearEndFinalized { get; set; }
        public bool IsYearEndLocked { get; set; }
        public DateTime? YearEndFinalizedAt { get; set; }
        public string? YearEndFinalizedBy { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // Audit
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
