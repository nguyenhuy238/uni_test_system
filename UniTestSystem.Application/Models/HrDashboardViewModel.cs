namespace UniTestSystem.Application.Models
{
    public class DashboardViewModel
    {
        // Cards
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalTests { get; set; }
        public int ActiveAssignments { get; set; }
        public double PassRatePercent { get; set; }

        // Tables
        public List<ActiveAssignmentRow> ActiveAssignmentsList { get; set; } = new();
        public List<RecentSubmissionRow> RecentSubmissions { get; set; } = new();

        // Charts (top subject theo số câu đã làm)
        public List<SubjectStat> TopSubjects { get; set; } = new();

        public class ActiveAssignmentRow
        {
            public string TestId { get; set; } = "";
            public string TestTitle { get; set; } = "";
            public string Target { get; set; } = ""; // Class/Course/Student
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
        }
        public class RecentSubmissionRow
        {
            public string SessionId { get; set; } = "";
            public string UserName { get; set; } = "";
            public string TestTitle { get; set; } = "";
            public double Score { get; set; }
            public bool IsPass { get; set; }
            public DateTime EndAt { get; set; }
        }
        public class SubjectStat
        {
            public string Subject { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
