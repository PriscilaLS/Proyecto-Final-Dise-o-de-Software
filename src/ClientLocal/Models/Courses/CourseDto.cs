using System.Text.Json.Serialization;

namespace ClientLocal.Models.Courses
{
    public class CourseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
    }
}