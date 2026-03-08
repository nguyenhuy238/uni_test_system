using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Session
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = "";

        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        [JsonPropertyName("testId")]
        public string TestId { get; set; } = "";

        [JsonPropertyName("testTitle")]
        public string TestTitle { get; set; } = "";

        [JsonPropertyName("startAt")]
        public DateTime? StartAt { get; set; }

        [JsonPropertyName("endAt")]
        public DateTime? EndAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";

        [JsonPropertyName("lastActivityAt")]
        public DateTime? LastActivityAt { get; set; }

        [JsonPropertyName("totalScore")]
        public decimal TotalScore { get; set; }

        [JsonPropertyName("maxScore")]
        public decimal MaxScore { get; set; }

        [JsonPropertyName("percent")]
        public decimal Percent { get; set; }

        [JsonPropertyName("isPassed")]
        public bool IsPassed { get; set; }
    }
}
