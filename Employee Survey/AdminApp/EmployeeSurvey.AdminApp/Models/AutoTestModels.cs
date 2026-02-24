using System.Collections.Generic;

namespace EmployeeSurvey.AdminApp.Models;

public class AutoTestOptions
{
    public string Mode { get; set; } = "Department"; 
    public string? Department { get; set; }
    public List<string>? UserIds { get; set; }
    public List<string>? Skills { get; set; }
    public string DifficultyPolicy { get; set; } = "ByLevel";
    public int McqCount { get; set; } = 8;
    public int TfCount { get; set; } = 2;
    public int MatchingCount { get; set; } = 0;
    public int DragDropCount { get; set; } = 0;
    public int EssayCount { get; set; } = 0;
    public decimal TotalScore { get; set; } = 10m;
    public bool FailWhenInsufficient { get; set; } = true;
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
}

public class PersonalizedTestResult
{
    public User User { get; set; } = new();
    public Test Test { get; set; } = new();
    // Simplified for client view
}

public class AssignBatchRequest
{
    public List<string> TestIds { get; set; } = new();
    public List<string> UserIds { get; set; } = new();
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
}
