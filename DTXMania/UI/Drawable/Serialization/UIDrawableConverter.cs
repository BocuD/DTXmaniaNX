using DTXUIRenderer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;

namespace DTXMania.UI.Drawable.Serialization;

public class UIDrawableConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(UIDrawable).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Use the default writer for serialization.");
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jObject = JObject.Load(reader);
        
        // Read the type name from the "type" property
        var typeName = jObject["type"]?.ToString();
        
        if (string.IsNullOrEmpty(typeName))
        {
            throw new JsonSerializationException("Type name is missing in the JSON.");
        }
        
        var targetType = Type.GetType(typeName);
        if (targetType == null)
        {
            throw new JsonSerializationException($"Type {typeName} not found.");
        }
        
        // Deserialize the object into the correct type
        var result = Activator.CreateInstance(targetType);
        serializer.Populate(jObject.CreateReader(), result);

        //type specific post serialization methods
        switch (result)
        {
            case UIText text:
                text.UpdateFont();
                text.RenderTexture();
                break;
            
            // If it's a UIGroup, set the parent field on each child
            case UIGroup uiGroup:
            {
                foreach (var child in uiGroup.children)
                {
                    if (child is UIDrawable childDrawable)
                    {
                        var field = childDrawable.GetType().GetField("parent", BindingFlags.NonPublic | BindingFlags.Instance);
                        field?.SetValue(childDrawable, uiGroup);
                    }
                }
                break;
            }

            case UIImage image:
            {
                image.LoadResource();
                break;
            }
        }

        return result;
    }
}

