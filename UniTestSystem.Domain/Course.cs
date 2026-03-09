using System;
using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class Course
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MinLength(2)]
        public string Name { get; set; } = "";

        [Required]
        public string Code { get; set; } = "";

        public int Credits { get; set; } = 3;

        public string LecturerId { get; set; } = "";
        public virtual User? Lecturer { get; set; }

        public string SubjectArea { get; set; } = "";
        public string Semester { get; set; } = "";

        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
        public virtual ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
