namespace UniTestSystem.Domain
{
    public class Answer
    {
        public string QuestionId { get; set; } = "";
        public string? Selected { get; set; }   // cho MCQ/TF
        public decimal Score { get; set; }       // điểm cho câu hỏi
        public string? TextAnswer { get; set; } // cho Essay
    }
}
