namespace UniTestSystem.Application.Models
{
    public class RegradeRequestItemVm
    {
        public string SessionId { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string TestId { get; set; } = "";
        public string TestTitle { get; set; } = "";
        public DateTime RequestedAt { get; set; }
        public string Reason { get; set; } = "";
        public bool IsGradeLocked { get; set; }
    }
}
