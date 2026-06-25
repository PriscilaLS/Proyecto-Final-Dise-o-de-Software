using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClientLocal.Models.Submission;

namespace ClientLocal.Services.Api
{
    public class SubmissionRepository
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public SubmissionRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        public async Task<SubmitTaskResponse> SubmitProjectAsync(int taskId, string zipPath)
        {
            using var form = new MultipartFormDataContent();
            using var stream = File.OpenRead(zipPath);
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            form.Add(fileContent, "project", Path.GetFileName(zipPath));

            var response = await _httpClient.PostAsync($"tasks/{taskId}/submit", form);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonSerializer.Deserialize<SubmitTaskResponse>(body, _jsonOptions) ?? new SubmitTaskResponse();

                if (!response.IsSuccessStatusCode)
                    result.Error ??= $"Error {response.StatusCode}: {body}";
                else if (result.SubmissionId <= 0 && result.Id > 0)
                    result.SubmissionId = result.Id;

                return result;
            }
            catch (JsonException ex)
            {
                return new SubmitTaskResponse
                {
                    Error = $"Respuesta JSON invalida del backend: {ex.Message}. Respuesta: {body}"
                };
            }
        }

        public async Task<List<SubmissionVersionDto>> GetMySubmissionsByTaskAsync(int taskId)
        {
            var response = await _httpClient.GetAsync($"tasks/{taskId}/my-submissions");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return DeserializeListResponse<SubmissionVersionDto>(body, "versions");
        }

        public async Task<List<SubmissionSummaryDto>> GetSubmissionsByTaskAsync(int taskId)
        {
            var response = await _httpClient.GetAsync($"tasks/{taskId}/submissions");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return DeserializeListResponse<SubmissionSummaryDto>(body, "submissions");
        }

        public async Task<List<SubmissionVersionDto>> GetVersionsBySubmissionAsync(int submissionId)
        {
            var response = await _httpClient.GetAsync($"submissions/{submissionId}/versions");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return DeserializeListResponse<SubmissionVersionDto>(body, "versions");
        }

        private List<T> DeserializeListResponse<T>(string body, string preferredProperty)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<List<T>>(root.GetRawText(), _jsonOptions)
                           ?? new List<T>();

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetError(root, out var error))
                        throw new Exception(error);

                    var candidateNames = new[] { preferredProperty, "data", "items", "result" };
                    foreach (var name in candidateNames.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (TryGetProperty(root, name, out var property) &&
                            property.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<T>>(property.GetRawText(), _jsonOptions)
                                   ?? new List<T>();
                        }
                    }
                }

                throw new Exception($"Respuesta inesperada del backend: {body}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Respuesta JSON invalida del backend: {ex.Message}. Respuesta: {body}");
            }
        }

        private static bool TryGetError(JsonElement root, out string error)
        {
            error = string.Empty;

            if (!TryGetProperty(root, "error", out var property))
                return false;

            error = property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? "Error del backend"
                : property.GetRawText();

            return !string.IsNullOrWhiteSpace(error);
        }

        private static bool TryGetProperty(JsonElement root, string name, out JsonElement property)
        {
            foreach (var item in root.EnumerateObject())
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }
    }
}
