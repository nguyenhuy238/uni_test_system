using System;

namespace EmployeeSurvey.AdminApp.Models;

public class Draft
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = string.Empty; // "Question" or "Test"
    public string Title { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
