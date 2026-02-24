namespace EmployeeSurvey.AdminApp.Models;

public enum QType { MCQ, TrueFalse, Matching, DragDrop, Essay }

public class Question
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public QType Type { get; set; }
    public string Skill { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
