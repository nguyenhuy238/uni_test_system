namespace Employee_Survey.Domain
{
    public class UserAnswer
    {
        public string UserId { get; set; } = "";
        public string QuestionId { get; set; } = "";
        public string OptionId { get; set; } = "";
        public string AnswerText { get; set; } = ""; // For essay questions
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
