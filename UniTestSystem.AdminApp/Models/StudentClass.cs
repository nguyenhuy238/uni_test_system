using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class StudentClass
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("facultyId")]
        public string? FacultyId { get; set; }
    }
}
