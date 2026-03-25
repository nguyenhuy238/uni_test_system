using System;

namespace UniTestSystem.Domain
{
    public class Enrollment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string StudentId { get; set; } = "";
        public virtual User? Student { get; set; }

        public string CourseId { get; set; } = "";
        public virtual Course? Course { get; set; }

        public string Semester { get; set; } = "";

        // Grading results
        public decimal? FinalScore { get; set; } // 0..10
        public string? Grade { get; set; } // A, B, C, D, F
        public decimal? GradePoint { get; set; } // 4.0 scale

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
