using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Automatizacion.AgentesKoncilia.Core.Converters
{
    /// <summary>
    /// Converter que maneja tanto strings como arrays de strings.
    /// Convierte arrays a strings unidos por ", ".
    /// </summary>
    public class StringOrArrayConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString() ?? "";

                case JsonTokenType.StartArray:
                    var items = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;

                        if (reader.TokenType == JsonTokenType.String)
                        {
                            items.Add(reader.GetString() ?? "");
                        }
                    }
                    return string.Join(", ", items);

                default:
                    return "";
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
