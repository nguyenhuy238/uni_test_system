using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class QuestionMetadataResponse
    {
        [JsonPropertyName("skills")]
        public List<QuestionMetadataItem> Skills { get; set; } = new();

        [JsonPropertyName("difficulties")]
        public List<QuestionMetadataItem> Difficulties { get; set; } = new();
    }

    public class QuestionMetadataItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
