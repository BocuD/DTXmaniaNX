using DTXMania.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Animation;

/// <summary>
/// Writes a clip that lives in a file as a reference to it, and one that lives in the layout in full.
/// Only the layout serializer uses this: a clip file has to hold the whole clip, or there would be
/// nothing to reference.
/// </summary>
public sealed class AnimationClipConverter : JsonConverter<AnimationClip>
{
    public override void WriteJson(JsonWriter writer, AnimationClip? clip, JsonSerializer serializer)
    {
        if (clip == null)
        {
            writer.WriteNull();
            return;
        }

        if (clip.IsEmbedded)
        {
            JObject.FromObject(clip, ClipSerializer).WriteTo(writer);
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("clipSource");
        writer.WriteValue(clip.clipSource.ToString());
        writer.WritePropertyName("resource");
        writer.WriteValue(clip.resource);
        writer.WriteEndObject();
    }

    public override AnimationClip? ReadJson(JsonReader reader, Type objectType, AnimationClip? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        JObject json = JObject.Load(reader);

        //a reference carries nothing but where to find the clip, so the clip itself comes from there
        if (json["tracks"] == null && Enum.TryParse(json["clipSource"]?.ToString(), out ClipSource source))
        {
            string resource = json["resource"]?.ToString() ?? string.Empty;

            return source == ClipSource.Skin
                ? LoadFromCurrentSkin(resource)
                : AnimationClipIO.LoadFromSystem(resource);
        }

        return json.ToObject<AnimationClip>(ClipSerializer);
    }

    private static AnimationClip? LoadFromCurrentSkin(string resource)
        => CDTXMania.SkinManager.currentSkin is { } skin
            ? AnimationClipIO.LoadFromSkin(skin, resource)
            : null;

    //a plain serializer, since going through the one that owns this converter would come straight back here
    private static readonly JsonSerializer ClipSerializer = new();
}
