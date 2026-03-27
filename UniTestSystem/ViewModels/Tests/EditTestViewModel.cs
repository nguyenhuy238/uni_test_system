using UniTestSystem.Application;
using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Tests;

public class EditTestViewModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? CourseId { get; set; }
    public int DurationMinutes { get; set; }
    public int PassScore { get; set; }
    public AssessmentType AssessmentType { get; set; } = AssessmentType.Quiz;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool IsPublished { get; set; }

    public PagedResult<Question> Page { get; set; } = new();
    public QuestionFilter Filter { get; set; } = new();
    public List<string> SelectedQuestionIds { get; set; } = new();

    public List<Question> MCQQuestions => Page.Items?.FindAll(q => q.Type == QType.MCQ) ?? new();
    public List<Question> TFQuestions => Page.Items?.FindAll(q => q.Type == QType.TrueFalse) ?? new();
    public List<Question> EssayQuestions => Page.Items?.FindAll(q => q.Type == QType.Essay) ?? new();
    public List<Question> MatchingQuestions => Page.Items?.FindAll(q => q.Type == QType.Matching) ?? new();
    public List<Question> DragDropQuestions => Page.Items?.FindAll(q => q.Type == QType.DragDrop) ?? new();
}
