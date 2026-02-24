namespace EmployeeSurvey.AdminApp.Models;

public enum TestType { Test, Survey }

public class Test
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TestType Type { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
