using System;
using System.Collections.Generic;
using System.Net.Http;
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
    }
}