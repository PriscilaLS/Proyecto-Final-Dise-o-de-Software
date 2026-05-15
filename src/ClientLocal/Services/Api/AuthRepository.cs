using System.Net.Http;
using System.Text;
using System.Text.Json;
using ClientLocal.Models.Auth;

namespace ClientLocal.Services.Api
{
    public class AuthRepository
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("auth/login", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponse
                {
                    Success = false,
                    Mensaje = $"Error {response.StatusCode}: {body}"
                };
            }

            var result = JsonSerializer.Deserialize<AuthResponse>(body, _jsonOptions);
            return result ?? new AuthResponse
            {
                Success = false,
                Mensaje = "Respuesta vacía del servidor."
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("auth/register", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AuthResponse
                {
                    Success = false,
                    Mensaje = $"Error {response.StatusCode}: {body}"
                };
            }

            var result = JsonSerializer.Deserialize<AuthResponse>(body, _jsonOptions);
            return result ?? new AuthResponse
            {
                Success = false,
                Mensaje = "Respuesta vacía del servidor."
            };
        }
    }
}