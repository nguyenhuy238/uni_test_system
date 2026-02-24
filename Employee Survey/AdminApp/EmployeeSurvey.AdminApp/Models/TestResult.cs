using System;

namespace EmployeeSurvey.AdminApp.Models;

public class TestResult
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public TestType TestType { get; set; }
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public DateTime SubmitTime { get; set; }
    public ResultStatus Status { get; set; }
    
    public string ScoreDisplay => $"{Score}/{MaxScore}";
}

public enum ResultStatus
{
    Submitted = 0,
    Grading = 1,
    Completed = 2
}
