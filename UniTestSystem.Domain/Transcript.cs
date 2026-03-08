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

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // Audit
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
