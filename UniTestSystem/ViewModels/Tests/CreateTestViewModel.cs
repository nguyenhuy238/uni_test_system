using UniTestSystem.Application;
using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Tests;

public class CreateTestViewModel
{
    public PagedResult<Question> Page { get; set; } = new();
    public QuestionFilter Filter { get; set; } = new();

    public string Title { get; set; } = "";
    public string? CourseId { get; set; }
    public int DurationMinutes { get; set; } = 10;
    public int PassScore { get; set; } = 3;
    public AssessmentType AssessmentType { get; set; } = AssessmentType.Quiz;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleOptions { get; set; } = true;

    public List<string> SelectedQuestionIds { get; set; } = new();

    public List<Question> MCQQuestions => Page.Items?.FindAll(q => q.Type == QType.MCQ) ?? new();
    public List<Question> TFQuestions => Page.Items?.FindAll(q => q.Type == QType.TrueFalse) ?? new();
    public List<Question> EssayQuestions => Page.Items?.FindAll(q => q.Type == QType.Essay) ?? new();
    public List<Question> MatchingQuestions => Page.Items?.FindAll(q => q.Type == QType.Matching) ?? new();
    public List<Question> DragDropQuestions => Page.Items?.FindAll(q => q.Type == QType.DragDrop) ?? new();
}
