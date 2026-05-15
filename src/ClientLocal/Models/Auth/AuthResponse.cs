using System.Text.Json.Serialization;

namespace ClientLocal.Models.Auth
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("rol")]
        public string? Rol { get; set; }

        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}