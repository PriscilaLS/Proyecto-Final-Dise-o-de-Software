using System.Text.Json.Serialization;

namespace ClientLocal.Models.Auth
{
    public class LoginRequest
    {
        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string Contrasena { get; set; } = string.Empty;
    }
}