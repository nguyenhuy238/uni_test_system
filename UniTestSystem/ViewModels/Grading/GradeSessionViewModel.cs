using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Grading;

public class GradeSessionViewModel
{
    public Session Session { get; set; } = new();
    public Test Test { get; set; } = new();
    public List<EssayItem> Essays { get; set; } = new();

    public class EssayItem
    {
        public string QuestionId { get; set; } = "";
        public string Content { get; set; } = "";
        public decimal MaxPoints { get; set; } = 0m;
        public string? UserAnswer { get; set; }
        public decimal? GivenScore { get; set; }
        public string? Comment { get; set; }
    }
}
