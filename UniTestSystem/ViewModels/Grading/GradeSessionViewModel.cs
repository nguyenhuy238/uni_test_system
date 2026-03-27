using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Grading;

public class GradeSessionViewModel
{
    public Session Session { get; set; } = new();
    public Test Test { get; set; } = new();
    public List<AnswerItem> Answers { get; set; } = new();

    public class AnswerItem
    {
        public string QuestionId { get; set; } = "";
        public QType Type { get; set; } = QType.MCQ;
        public string TypeLabel { get; set; } = "";
        public string Content { get; set; } = "";
        public decimal MaxPoints { get; set; } = 0m;
        public string? UserAnswerDisplay { get; set; }
        public string? CorrectAnswerDisplay { get; set; }
        public decimal GivenScore { get; set; }
        public decimal? AutoSuggestedScore { get; set; }
        public bool IsAutoGradable { get; set; }
        public bool IsManuallyGraded { get; set; }
        public string? Comment { get; set; }
    }
}
