using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Enrollment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("studentId")]
        public string StudentId { get; set; } = "";

        [JsonPropertyName("courseId")]
        public string CourseId { get; set; } = "";

        [JsonPropertyName("semester")]
        public string Semester { get; set; } = "";

        [JsonPropertyName("enrolledAt")]
        public DateTime EnrolledAt { get; set; }

        [JsonPropertyName("userName")] // Helper for display
        public string? StudentName { get; set; }
    }
}
