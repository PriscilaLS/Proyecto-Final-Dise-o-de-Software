using System;
using System.Collections.Generic;
using System.Net.Http;
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
    }
}