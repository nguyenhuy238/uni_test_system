using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class BackupFileInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("length")]
        public long Length { get; set; }

        [JsonPropertyName("lastWriteTimeUtc")]
        public DateTime LastWriteTimeUtc { get; set; }
    }

    public class BackupResult
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("backupPath")]
        public string? BackupPath { get; set; }

        [JsonPropertyName("file")]
        public string? File { get; set; }
    }
}
