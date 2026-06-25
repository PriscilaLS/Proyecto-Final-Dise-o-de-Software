using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientLocal.Models.Submission
{
    public class FlexibleBooleanJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.TryGetInt32(out var value) && value != 0,
                JsonTokenType.String => ParseString(reader.GetString()),
                _ => false
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }

        private static bool ParseString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue != 0;

            return false;
        }
    }
}
