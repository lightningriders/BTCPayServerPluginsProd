using System;
using Newtonsoft.Json;

public class ToStringJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(long) || objectType == typeof(int);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var str = reader.Value?.ToString();
        if (string.IsNullOrEmpty(str))
            throw new JsonSerializationException("Expected a non-empty string.");

        if (objectType == typeof(long))
            return long.Parse(str);
        if (objectType == typeof(int))
            return int.Parse(str);

        throw new NotSupportedException($"Cannot convert to {objectType}");
    }

}
