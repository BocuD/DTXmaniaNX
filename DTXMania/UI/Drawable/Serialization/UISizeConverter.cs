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
        WriteAxis(writer, "X", size.X, size.xMode, size.marginLeft, size.marginRight);
        WriteAxis(writer, "Y", size.Y, size.yMode, size.marginTop, size.marginBottom);
        writer.WriteEndObject();
    }

    private static void WriteAxis(JsonWriter writer, string axis, float value, UiSizeMode mode,
        float marginStart, float marginEnd)
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
                WriteMargin(writer, axis + "MarginStart", marginStart);
                WriteMargin(writer, axis + "MarginEnd", marginEnd);
                break;
        }
    }

    //a margin nobody set stays out of the file, so an inheriting axis still writes as just its mode
    private static void WriteMargin(JsonWriter writer, string name, float margin)
    {
        if (margin == 0f)
        {
            return;
        }

        writer.WritePropertyName(name);
        writer.WriteValue(margin);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        UISize size = existingValue is UISize current ? current : default;

        if (reader.TokenType == JsonToken.Null)
        {
            return size;
        }

        JObject jObject = JObject.Load(reader);
        ReadAxis(jObject, "X", ref size,
            static (ref UISize s, float v) => s.X = v,
            static (ref UISize s, UiSizeMode m) => s.xMode = m,
            static (ref UISize s, float v) => s.marginLeft = v,
            static (ref UISize s, float v) => s.marginRight = v);
        ReadAxis(jObject, "Y", ref size,
            static (ref UISize s, float v) => s.Y = v,
            static (ref UISize s, UiSizeMode m) => s.yMode = m,
            static (ref UISize s, float v) => s.marginTop = v,
            static (ref UISize s, float v) => s.marginBottom = v);
        return size;
    }

    private delegate void AxisValueSetter(ref UISize size, float value);
    private delegate void AxisModeSetter(ref UISize size, UiSizeMode mode);

    private static void ReadAxis(JObject jObject, string axis, ref UISize size, AxisValueSetter setValue,
        AxisModeSetter setMode, AxisValueSetter setMarginStart, AxisValueSetter setMarginEnd)
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

        ReadMargin(jObject, axis + "MarginStart", ref size, setMarginStart);
        ReadMargin(jObject, axis + "MarginEnd", ref size, setMarginEnd);
    }

    private static void ReadMargin(JObject jObject, string name, ref UISize size, AxisValueSetter set)
    {
        if (jObject[name] is { Type: not JTokenType.Null } margin)
        {
            set(ref size, margin.Value<float>());
        }
    }
}
