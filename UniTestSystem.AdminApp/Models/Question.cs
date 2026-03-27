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

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";

        [JsonPropertyName("options")]
        public List<Option>? Options { get; set; } = new();

        [JsonIgnore]
        public List<string>? CorrectKeys { get; set; } = new();

        [JsonPropertyName("skillId")]
        public string Skill { get; set; } = "General";

        [JsonPropertyName("difficultyLevelId")]
        public string Difficulty { get; set; } = "Junior";

        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = "";

        [JsonPropertyName("mediaUrl")]
        public string? MediaUrl { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
