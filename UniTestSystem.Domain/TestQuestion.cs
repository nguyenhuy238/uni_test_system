using System;

namespace UniTestSystem.Domain
{
    public class TestQuestion
    {
        public string TestId { get; set; } = "";
        public virtual Test Test { get; set; } = null!;

        public string QuestionId { get; set; } = "";
        public virtual Question Question { get; set; } = null!;

        public decimal Points { get; set; } = 1.0m;
        public int Order { get; set; }
    }
}
