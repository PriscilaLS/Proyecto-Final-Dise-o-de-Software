using System.Text.Json.Serialization;

namespace ClientLocal.Models.Tasks
{
    public class TaskDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("fechaLimite")]
        public string? FechaLimite { get; set; }
    }
}