using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class TestItem
    {
        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = "";

        [JsonPropertyName("points")]
        public decimal Points { get; set; } = 1m;
    }
}
