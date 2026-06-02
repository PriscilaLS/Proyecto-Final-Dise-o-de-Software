using System.Text.Json.Serialization;

namespace ClientLocal.Models.Courses
{
    public class CourseDto
    {
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("join_code")]
        public string? JoinCode { get; set; }
    }
}
