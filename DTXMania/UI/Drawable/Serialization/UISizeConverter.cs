using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Drawable.Serialization;

/// <summary>
/// Writes a claimed axis as its value (<c>"X": 300</c>), a driven one as its mode
/// (<c>"XMode": "Inherit"</c>), and one that follows the content not at all.
/// </summary>
public sealed class UISizeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(UISize);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not UISize size)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        WriteAxis(writer, "X", size.X, size.xMode);
        WriteAxis(writer, "Y", size.Y, size.yMode);
        writer.WriteEndObject();
    }

    private static void WriteAxis(JsonWriter writer, string axis, float value, UiSizeMode mode)
    {
        switch (mode)
        {
            case UiSizeMode.Fixed:
                writer.WritePropertyName(axis);
                writer.WriteValue(value);
                break;

            case UiSizeMode.Auto:
                break;

            default:
                writer.WritePropertyName(axis + "Mode");
                writer.WriteValue(mode.ToString());
                break;
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        UISize size = existingValue is UISize current ? current : default;

        if (reader.TokenType == JsonToken.Null)
        {
            return size;
        }

        JObject jObject = JObject.Load(reader);
        ReadAxis(jObject, "X", ref size, static (ref UISize s, float v) => s.X = v, static (ref UISize s, UiSizeMode m) => s.xMode = m);
        ReadAxis(jObject, "Y", ref size, static (ref UISize s, float v) => s.Y = v, static (ref UISize s, UiSizeMode m) => s.yMode = m);
        return size;
    }

    private delegate void AxisValueSetter(ref UISize size, float value);
    private delegate void AxisModeSetter(ref UISize size, UiSizeMode mode);

    private static void ReadAxis(JObject jObject, string axis, ref UISize size, AxisValueSetter setValue, AxisModeSetter setMode)
    {
        //assigning the value is itself what marks the axis Fixed, so it is read first
        if (jObject[axis] is { Type: not JTokenType.Null } value)
        {
            setValue(ref size, value.Value<float>());
            return;
        }

        if (jObject[axis + "Mode"] is { Type: not JTokenType.Null } mode
            && Enum.TryParse(mode.Value<string>(), out UiSizeMode parsed))
        {
            setMode(ref size, parsed);
        }
    }
}
