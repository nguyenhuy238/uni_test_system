using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class DashboardStats
    {
        [JsonPropertyName("totalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("totalTests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("totalSubmissions")]
        public int TotalSubmissions { get; set; }

        [JsonPropertyName("activeTestCount")]
        public int ActiveTestCount { get; set; }
    }
}
