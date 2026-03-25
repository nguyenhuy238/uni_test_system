using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class SystemSettingsModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "settings";

        [JsonPropertyName("systemName")]
        public string SystemName { get; set; } = "UniTestSystem";

        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("currentSemester")]
        public string? CurrentSemester { get; set; }

        [JsonPropertyName("currentAcademicYear")]
        public string? CurrentAcademicYear { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get; set; }
    }
}
