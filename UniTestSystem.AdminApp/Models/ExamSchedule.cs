using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class ExamSchedule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("testId")]
        public string TestId { get; set; } = "";

        [JsonPropertyName("courseId")]
        public string CourseId { get; set; } = "";

        [JsonPropertyName("room")]
        public string Room { get; set; } = "";

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("examType")]
        public string ExamType { get; set; } = "Final";

        [JsonPropertyName("course")]
        public Course? Course { get; set; }

        [JsonPropertyName("test")]
        public Test? Test { get; set; }
    }

    public class ExamScheduleDraft
    {
        public string TestId { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string Room { get; set; } = "";
        public DateTime StartTime { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(8);
        public DateTime EndTime { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(10);
        public string ExamType { get; set; } = "Final";
    }
}
