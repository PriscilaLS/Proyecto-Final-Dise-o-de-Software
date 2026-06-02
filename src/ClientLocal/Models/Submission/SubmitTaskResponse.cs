using System.Text.Json.Serialization;

namespace ClientLocal.Models.Submission
{
    public class SubmitTaskResponse
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("is_late")]
        public bool IsLate { get; set; }

        [JsonPropertyName("submitted_at")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
