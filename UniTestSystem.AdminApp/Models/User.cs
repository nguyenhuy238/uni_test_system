using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
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
        public string? Department { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("skill")]
        public string? Skill { get; set; }

        [JsonPropertyName("teamId")]
        public string TeamId { get; set; } = "";

        [JsonPropertyName("facultyId")]
        public string? FacultyId { get; set; }
    }
}
