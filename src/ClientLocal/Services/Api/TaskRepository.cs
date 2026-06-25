using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClientLocal.Models.Tasks;

namespace ClientLocal.Services.Api
{
    public class TaskRepository
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TaskRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<TaskDto>> GetTasksByCourseAsync(int courseId)
        {
            var response = await _httpClient.GetAsync($"courses/{courseId}/tasks");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<List<TaskDto>>(body, _jsonOptions) ?? new List<TaskDto>();
        }

        public async Task<TaskAttachmentDownload> DownloadAttachmentAsync(int taskId)
        {
            var response = await _httpClient.GetAsync($"tasks/{taskId}/attachment");
            var content = await response.Content.ReadAsByteArrayAsync();

            if (!response.IsSuccessStatusCode)
            {
                var body = Encoding.UTF8.GetString(content);
                throw new Exception($"Error {response.StatusCode}: {body}");
            }

            return new TaskAttachmentDownload
            {
                Content = content,
                FileName = GetAttachmentFileName(response, taskId)
            };
        }

        private static string GetAttachmentFileName(HttpResponseMessage response, int taskId)
        {
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName;

            fileName = fileName?.Trim('"');
            fileName = string.IsNullOrWhiteSpace(fileName)
                ? $"task_{taskId}_attachment"
                : Path.GetFileName(fileName);

            return string.IsNullOrWhiteSpace(fileName) ? $"task_{taskId}_attachment" : fileName;
        }
    }

    public class TaskAttachmentDownload
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
