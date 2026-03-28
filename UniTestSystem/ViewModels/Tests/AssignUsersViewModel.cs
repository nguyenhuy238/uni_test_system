using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Tests;

public class AssignUsersViewModel
{
    public string TestId { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string LecturerName { get; set; } = string.Empty;
    public int TotalEnrolled { get; set; }
    public List<StudentClass> AvailableClasses { get; set; } = new();
    public string? SelectedClassId { get; set; }
    public bool IsOwner { get; set; }

    public List<User> Users { get; set; } = new();
    public HashSet<string> AssignedUserIds { get; set; } = new();

    public List<string> Faculties { get; set; } = new();
    public string? SelectedFaculty { get; set; }
}
