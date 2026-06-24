using System.Text.Json.Serialization;

namespace ClientLocal.Models.Submission
{
    public class SubmissionSummaryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("submission_id")]
        public int SubmissionId { get; set; }

        [JsonPropertyName("task_id")]
        public int TaskId { get; set; }

        [JsonPropertyName("student_id")]
        public int StudentId { get; set; }

        [JsonPropertyName("student")]
        public string? Student { get; set; }

        [JsonPropertyName("latest_version_id")]
        public int LatestVersionId { get; set; }

        [JsonPropertyName("latest_version_number")]
        public int LatestVersionNumber { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        [JsonPropertyName("submitted_at")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("is_late")]
        public bool IsLate { get; set; }

        [JsonPropertyName("total_versions")]
        public int TotalVersions { get; set; }

        public string StatusText => IsLate ? "Tardia" : "A tiempo";

        public string DisplayText => $"{Student} | ultima v{LatestVersionNumber} | {SubmittedAt} | {StatusText} | {TotalVersions} version(es)";
    }
}
