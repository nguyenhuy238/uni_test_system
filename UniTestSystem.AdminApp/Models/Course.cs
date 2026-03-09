using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Course
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("credits")]
        public int Credits { get; set; } = 3;

        [JsonPropertyName("lecturerId")]
        public string? LecturerId { get; set; }

        [JsonPropertyName("subjectArea")]
        public string SubjectArea { get; set; } = "";

        [JsonPropertyName("semester")]
        public string Semester { get; set; } = "";
    }
}
