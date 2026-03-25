namespace UniTestSystem.Application.Models
{
    public class TranscriptAdminRowVm
    {
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string? ClassId { get; set; }
        public string ClassName { get; set; } = "(Unassigned)";
        public string? FacultyId { get; set; }
        public string FacultyName { get; set; } = "(Unassigned)";
        public decimal GPA { get; set; }
        public int TotalCredits { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
