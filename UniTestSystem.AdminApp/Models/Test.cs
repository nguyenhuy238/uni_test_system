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

        [JsonPropertyName("totalMaxScore")]
        public decimal TotalMaxScore { get; set; } = 10;

        [JsonPropertyName("isPublished")]
        public bool IsPublished { get; set; } = false;

        [JsonPropertyName("items")]
        public List<TestItem>? Items { get; set; } = new();

        [JsonPropertyName("questionIds")]
        public List<string>? QuestionIds { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
