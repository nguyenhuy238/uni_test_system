using System.Text.Json.Serialization;

namespace EmployeeSurvey.AdminApp.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("role")]
        public string Role { get; set; } = "User";

        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("level")]
        public string Level { get; set; } = "Junior";

        [JsonPropertyName("skill")]
        public string Skill { get; set; } = "C#";

        [JsonPropertyName("teamId")]
        public string TeamId { get; set; } = "";
    }
}
