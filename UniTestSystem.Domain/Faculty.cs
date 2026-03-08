using System;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class Faculty
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MinLength(2)]
        public string Name { get; set; } = "";

        public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
        public virtual ICollection<Lecturer> Lecturers { get; set; } = new List<Lecturer>();

        [MaxLength(512)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
