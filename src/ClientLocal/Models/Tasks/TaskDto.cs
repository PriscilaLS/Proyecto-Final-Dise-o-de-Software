using System.Text.Json.Serialization;

namespace ClientLocal.Models.Tasks
{
    public class TaskDto
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("due_date")]
        public string? DueDate { get; set; }
    }
}
