using DTXMania.UI.Skin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Animation;

/// <summary>
/// Writes a clip that lives in a file as a reference to it, and one that lives in the layout in full.
/// Only the layout serializer uses this: a clip file has to hold the whole clip, or there would be
/// nothing to reference.
/// </summary>
public sealed class AnimatorClipConverter : JsonConverter<AnimatorClip>
{
    public override void WriteJson(JsonWriter writer, AnimatorClip? entry, JsonSerializer serializer)
    {
        if (entry == null)
        {
            writer.WriteNull();
            return;
        }

        if (entry.IsEmbedded)
        {
            JObject.FromObject(entry.clip, ClipSerializer).WriteTo(writer);
            return;
        }

        JObject.FromObject(entry.resource, ClipSerializer).WriteTo(writer);
    }

    public override AnimatorClip? ReadJson(JsonReader reader, Type objectType, AnimatorClip? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        JObject json = JObject.Load(reader);

        //a reference carries nothing but where to find the clip, so the clip itself comes from there
        if (json["tracks"] == null && json["path"] != null)
        {
            SkinResource resource = json.ToObject<SkinResource>(ClipSerializer);

            //a file that has gone missing keeps its reference rather than dropping out of the layout,
            //so saving again does not quietly discard it
            AnimationClip clip = AnimationClipIO.Load(resource)
                                 ?? new AnimationClip { name = Path.GetFileNameWithoutExtension(resource.path) };

            return new AnimatorClip(clip, resource);
        }

        return new AnimatorClip(json.ToObject<AnimationClip>(ClipSerializer) ?? new AnimationClip());
    }

    //a plain serializer, since going through the one that owns this converter would come straight back here
    private static readonly JsonSerializer ClipSerializer = new();
}
