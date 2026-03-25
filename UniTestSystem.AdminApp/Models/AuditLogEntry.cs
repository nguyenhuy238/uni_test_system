using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class AuditLogEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("at")]
        public DateTime At { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; } = "";

        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("entityName")]
        public string EntityName { get; set; } = "";

        [JsonPropertyName("entityId")]
        public string EntityId { get; set; } = "";

        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("after")]
        public string? After { get; set; }
    }
}
