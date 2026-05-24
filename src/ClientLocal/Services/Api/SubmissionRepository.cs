using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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
                PropertyNameCaseInsensitive = true
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

                return result;
            }
            catch
            {
                return new SubmitTaskResponse
                {
                    Error = $"Respuesta no JSON del backend: {body}"
                };
            }
        }
    }
}