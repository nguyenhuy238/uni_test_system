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

        [JsonPropertyName("warningGpaThreshold")]
        public decimal WarningGpaThreshold { get; set; } = 2.0m;

        [JsonPropertyName("failGpaThreshold")]
        public decimal FailGpaThreshold { get; set; } = 1.0m;

        [JsonPropertyName("treatOutstandingDebtAsFail")]
        public bool TreatOutstandingDebtAsFail { get; set; } = true;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get; set; }
    }
}
