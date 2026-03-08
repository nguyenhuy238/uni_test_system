using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class Subject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Required]
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DifficultyLevel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Required]
        public string Name { get; set; } = ""; // Easy, Medium, Hard
        public int Weight { get; set; } = 1;
        public bool IsDeleted { get; set; }
    }

    public class Skill
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Required]
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
