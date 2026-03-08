namespace UniTestSystem.Domain
{
    public class Lecturer : User
    {
        public string LecturerCode { get; set; } = "";
        public string? FacultyId { get; set; }
        public virtual Faculty? Faculty { get; set; }
    }
}
