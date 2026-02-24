using System;
using System.ComponentModel.DataAnnotations;

namespace Employee_Survey.Domain
{
    public class Department
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [Required, MinLength(2)]
        public string Name { get; set; } = "";

        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

        [MaxLength(512)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
