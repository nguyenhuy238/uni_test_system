using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Test
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; } = 30;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Test";

        [JsonPropertyName("courseId")]
        public string? CourseId { get; set; }

        [JsonPropertyName("assessmentType")]
        public string AssessmentType { get; set; } = "Quiz";

        [JsonPropertyName("totalMaxScore")]
        public decimal TotalMaxScore { get; set; } = 10;

        [JsonPropertyName("passScore")]
        public int PassScore { get; set; } = 5;

        [JsonPropertyName("shuffleQuestions")]
        public bool ShuffleQuestions { get; set; } = true;

        [JsonPropertyName("shuffleOptions")]
        public bool ShuffleOptions { get; set; } = true;

        [JsonPropertyName("isPublished")]
        public bool IsPublished { get; set; } = false;

        [JsonPropertyName("testQuestions")]
        public List<TestItem>? Items { get; set; } = new();

        [JsonIgnore]
        public List<string>? QuestionIds { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
