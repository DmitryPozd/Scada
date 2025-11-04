using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scada.Client.Services;

/// <summary>
/// Case-insensitive JSON converter for enums.
/// Handles JSON with lowercase keys and proper enum values.
/// </summary>
public class CaseInsensitiveEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var enumString = reader.GetString();
            if (enumString != null && Enum.TryParse<T>(enumString, ignoreCase: true, out var result))
            {
                return result;
            }
            
            throw new JsonException($"Unable to convert \"{enumString}\" to enum {typeof(T).Name}");
        }
        
        if (reader.TokenType == JsonTokenType.Number)
        {
            var enumInt = reader.GetInt32();
            return (T)(object)enumInt;
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing enum {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
