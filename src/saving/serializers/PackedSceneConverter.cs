using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Converter for PackedScene
/// </summary>
public partial class PackedSceneConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        serializer.Serialize(writer, ((PackedScene)value).ResourcePath);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var path = serializer.Deserialize<string>(reader);

        if (string.IsNullOrEmpty(path))
            return null;

        return GD.Load<PackedScene>(path);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(PackedScene);
    }
}
