using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Option
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; } = false;
    }
}
