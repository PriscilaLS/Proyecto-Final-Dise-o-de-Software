using System.Text.Json.Serialization;

namespace ClientLocal.Models.Submission
{
    public class SubmitTaskResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("submission_id")]
        public int SubmissionId { get; set; }

        [JsonPropertyName("version_id")]
        public int VersionId { get; set; }

        [JsonPropertyName("version_number")]
        public int VersionNumber { get; set; }

        [JsonPropertyName("is_late")]
        [JsonConverter(typeof(FlexibleBooleanJsonConverter))]
        public bool IsLate { get; set; }

        [JsonPropertyName("submitted_at")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        public string StatusText => IsLate ? "Tardia" : "A tiempo";
    }
}
