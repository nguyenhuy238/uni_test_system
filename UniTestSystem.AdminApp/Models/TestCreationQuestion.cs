using System.Text.Json.Serialization;

namespace UniTestSystem.AdminApp.Models
{
    public class TestCreationQuestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = "";

        [JsonPropertyName("difficultyLevelId")]
        public string DifficultyLevelId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("questionBankId")]
        public string QuestionBankId { get; set; } = "";
    }

    public class TestCreationQuestionPoolResponse
    {
        [JsonPropertyName("courseId")]
        public string CourseId { get; set; } = "";

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<TestCreationQuestion> Items { get; set; } = new();
    }
}
