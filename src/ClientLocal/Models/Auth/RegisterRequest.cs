using System.Text.Json.Serialization;

namespace ClientLocal.Models.Auth
{
    public class RegisterRequest
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido1")]
        public string Apellido1 { get; set; } = string.Empty;

        [JsonPropertyName("apellido2")]
        public string? Apellido2 { get; set; }

        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string Contrasena { get; set; } = string.Empty;

        [JsonPropertyName("carnet")]
        public string Carnet { get; set; } = string.Empty;
    }
}