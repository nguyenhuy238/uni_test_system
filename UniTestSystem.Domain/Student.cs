namespace UniTestSystem.Domain
{
    public class Student : User
    {
        public string StudentCode { get; set; } = "";
        public string? StudentClassId { get; set; }
        public virtual StudentClass? StudentClass { get; set; }
        public string AcademicYear { get; set; } = "";
        public string Major { get; set; } = "";
    }
}
