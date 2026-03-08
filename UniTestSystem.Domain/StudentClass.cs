using System;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class StudentClass
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MinLength(2)]
        public string Name { get; set; } = "";

        [Required, MinLength(2)]
        public string Code { get; set; } = "";

        /// <summary>Lớp thuộc khoa nào (có thể null nếu chưa gán)</summary>
        public string? FacultyId { get; set; }
        public virtual Faculty? Faculty { get; set; }

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
