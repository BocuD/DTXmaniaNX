using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace DTXMania.UI.Drawable.Serialization;

public class UIDrawableConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(UIDrawable).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jObject = JObject.Load(reader);
        
        // Read the type name from the "type" property
        string? typeName = jObject["type"]?.ToString();
        
        if (string.IsNullOrEmpty(typeName))
        {
            throw new JsonSerializationException("Type name is missing in the JSON.");
        }
        
        Type? targetType = Type.GetType(typeName);
        if (targetType == null)
        {
            throw new JsonSerializationException($"Type {typeName} not found.");
        }
        
        // Deserialize the object into the correct type
        var result = RuntimeHelpers.GetUninitializedObject(targetType);
        serializer.Populate(jObject.CreateReader(), result);
        
        UIDrawable drawable = (UIDrawable)result;
        drawable.OnDeserialize();
        return result;
    }
    
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Use the default writer for serialization.");
    }
}

