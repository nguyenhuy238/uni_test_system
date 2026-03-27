using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Tests;

public class AssignUsersViewModel
{
    public string TestId { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;

    public List<User> Users { get; set; } = new();
    public HashSet<string> AssignedUserIds { get; set; } = new();

    public List<string> Faculties { get; set; } = new();
    public string? SelectedFaculty { get; set; }
}
