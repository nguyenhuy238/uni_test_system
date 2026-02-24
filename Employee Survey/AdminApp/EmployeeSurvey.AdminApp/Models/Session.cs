using System;

namespace EmployeeSurvey.AdminApp.Models;

public class Session
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime LastActivityAt { get; set; }
    public double TotalScore { get; set; }
    public double MaxScore { get; set; }
    public double Percent { get; set; }
    public bool IsPassed { get; set; }
}

public enum SessionStatus
{
    Draft = 0,
    Started = 1,
    Completed = 2,
    Canceled = 3
}
