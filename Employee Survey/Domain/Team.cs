using System;
using System.ComponentModel.DataAnnotations;

namespace Employee_Survey.Domain
{
    public class Team
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MinLength(2)]
        public string Name { get; set; } = "";

        /// <summary>Team thuộc phòng ban nào (có thể null nếu chưa gán)</summary>
        public string? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
