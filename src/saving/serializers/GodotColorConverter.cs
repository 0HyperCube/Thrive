using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public partial class GodotColorConverter : JsonConverter
{
    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            throw new JsonException("Colour can't be null as it is a value type");

        var colour = (Color)value;

        writer.WriteStartObject();

        writer.WritePropertyName("r");
        serializer.Serialize(writer, colour.R);

        writer.WritePropertyName("g");
        serializer.Serialize(writer, colour.G);

        writer.WritePropertyName("b");
        serializer.Serialize(writer, colour.B);

        writer.WritePropertyName("a");
        serializer.Serialize(writer, colour.A);

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            return default(Color);
        }

        var item = JObject.Load(reader);

        try
        {
            // ReSharper disable AssignNullToNotNullAttribute
            return new Color(item["r"]!.Value<float>(),
                item["g"]!.Value<float>(),
                item["b"]!.Value<float>(),
                item["a"]!.Value<float>());

            // ReSharper restore AssignNullToNotNullAttribute
        }
        catch (Exception e) when (
            e is NullReferenceException or ArgumentNullException)
        {
            throw new JsonException("can't read Color (missing property)", e);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Color);
    }
}
