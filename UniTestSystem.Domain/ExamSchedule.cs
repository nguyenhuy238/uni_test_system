using System;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class ExamSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required]
        public string TestId { get; set; } = "";
        public virtual Test? Test { get; set; }

        [Required]
        public string CourseId { get; set; } = "";
        public virtual Course? Course { get; set; }

        [Required]
        public string Room { get; set; } = "";

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string ExamType { get; set; } = "Final";

        // Audit
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
