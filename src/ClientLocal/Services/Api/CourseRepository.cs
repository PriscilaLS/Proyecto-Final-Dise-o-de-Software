using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClientLocal.Models.Courses;

namespace ClientLocal.Services.Api
{
    public class CourseRepository
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public CourseRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<CourseDto>> GetEnrolledCoursesAsync()
        {
            var response = await _httpClient.GetAsync("courses/me");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error {response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<List<CourseDto>>(body, _jsonOptions) ?? new List<CourseDto>();
        }

        public async Task<string> JoinCourseAsync(string joinCode)
        {
            var payload = JsonSerializer.Serialize(new { join_code = joinCode });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("courses/join", content);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body, _jsonOptions)
                    ?? new Dictionary<string, string>();

                if (response.IsSuccessStatusCode)
                    return data.TryGetValue("message", out var message) ? message : "Te has unido al curso exitosamente.";

                if (data.TryGetValue("error", out var error))
                    throw new Exception(error);

                throw new Exception($"Error {response.StatusCode}: {body}");
            }
            catch (JsonException)
            {
                throw new Exception($"Respuesta no JSON del backend: {body}");
            }
        }
    }
}
