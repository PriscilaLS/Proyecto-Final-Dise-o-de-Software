using System.Text.Json.Serialization;

namespace ClientLocal.Models.Auth
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("user")]
        public AuthUserDto? User { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}