using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
                else if (result.SubmissionId <= 0 || result.VersionId <= 0 || result.VersionNumber <= 0)
                    result.Error = $"Respuesta de entrega incompleta del backend: {body}";

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

            return JsonSerializer.Deserialize<List<SubmissionVersionDto>>(body, _jsonOptions)
                   ?? new List<SubmissionVersionDto>();
        }

        public async Task<List<SubmissionSummaryDto>> GetSubmissionsByTaskAsync(int taskId)
        {
            var response = await _httpClient.GetAsync($"tasks/{taskId}/submissions");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<List<SubmissionSummaryDto>>(body, _jsonOptions)
                   ?? new List<SubmissionSummaryDto>();
        }

        public async Task<List<SubmissionVersionDto>> GetVersionsBySubmissionAsync(int submissionId)
        {
            var response = await _httpClient.GetAsync($"submissions/{submissionId}/versions");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<List<SubmissionVersionDto>>(body, _jsonOptions)
                   ?? new List<SubmissionVersionDto>();
        }
    }
}
