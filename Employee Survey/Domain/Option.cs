namespace Employee_Survey.Domain
{
    public class Option
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string QuestionId { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
    }
}
