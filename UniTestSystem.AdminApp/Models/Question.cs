using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Question
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "MCQ";

        [JsonPropertyName("options")]
        public List<string>? Options { get; set; } = new();

        [JsonPropertyName("correctKeys")]
        public List<string>? CorrectKeys { get; set; } = new();

        [JsonPropertyName("skill")]
        public string Skill { get; set; } = "General";

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "Junior";

        [JsonPropertyName("mediaUrl")]
        public string? MediaUrl { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
