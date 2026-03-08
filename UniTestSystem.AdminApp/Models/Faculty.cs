using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class Faculty
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
