using System.Text.Json.Serialization;

namespace ClientLocal.Models.Submission
{
    public class SubmissionVersionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("submission_id")]
        public int SubmissionId { get; set; }

        [JsonPropertyName("version_id")]
        public int VersionId { get; set; }

        [JsonPropertyName("version_number")]
        public int VersionNumber { get; set; }

        [JsonPropertyName("task_id")]
        public int TaskId { get; set; }

        [JsonPropertyName("student_id")]
        public int StudentId { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        [JsonPropertyName("submitted_at")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("is_late")]
        [JsonConverter(typeof(FlexibleBooleanJsonConverter))]
        public bool IsLate { get; set; }

        public string StatusText => IsLate ? "Tardia" : "A tiempo";

        public string DisplayText => $"v{VersionNumber} | {SubmittedAt} | {StatusText}";
    }
}
