using System.ComponentModel.DataAnnotations;
using UniTestSystem.Domain;

namespace UniTestSystem.ViewModels.Users;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required, Display(Name = "Full name")]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "Role")]
    public Role Role { get; set; } = Role.Student;

    [Required, Display(Name = "Academic Year")]
    public string AcademicYear { get; set; } = "2024";

    [Display(Name = "Class")]
    public string? StudentClassId { get; set; } = "";

    [Display(Name = "Faculty")]
    [StringLength(100)]
    public string? FacultyName { get; set; } = "";

    [Display(Name = "Major (for Students/Lecturers)")]
    [StringLength(100)]
    public string? Major { get; set; } = "";

    [Display(Name = "Student/Lecturer Code")]
    [StringLength(20)]
    public string? StudentCode { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Password (leave blank to keep)")]
    public string? Password { get; set; }
}
