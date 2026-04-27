using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTXMania.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Drawable.Serialization;

public class UIDrawableConverter : JsonConverter
{
    private static readonly ConcurrentDictionary<Type, List<FieldInfo>> ThemableFieldsCache = new();
    private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> ThemablePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, HashSet<string>> DeserializableNamesCache = new();

    public override bool CanConvert(Type objectType)
    {
        return typeof(UIDrawable).IsAssignableFrom(objectType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jObject = JObject.Load(reader);

        // Read the type name from the "type" property
        string? typeName = jObject["type"]?.ToString();
        string? id = jObject[nameof(UIDrawable.id)]?.ToString();

        if (string.IsNullOrEmpty(typeName))
        {
            throw new JsonSerializationException("Type name is missing in the JSON.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new JsonSerializationException("Drawable id is missing in the JSON.");
        }

        Type? targetType = Type.GetType(typeName);
        if (targetType == null)
        {
            throw new JsonSerializationException($"Type {typeName} not found.");
        }

        FilterNonThemableProperties(jObject, targetType);

        // Construct instance first to keep non-themable default values intact.
        object result = CreateDeserializationInstance(targetType);
        serializer.Populate(jObject.CreateReader(), result);

        UIDrawable drawable = (UIDrawable)result;
        drawable.OnDeserialize();
        return result;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not UIDrawable drawable || drawable.dontSerialize)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(drawable.type);
        writer.WritePropertyName(nameof(UIDrawable.id));
        writer.WriteValue(drawable.id);

        Type drawableType = drawable.GetType();
        HashSet<string> writtenNames = new(StringComparer.Ordinal);

        foreach (FieldInfo field in GetThemableFields(drawableType))
        {
            object? fieldValue = field.GetValue(drawable);
            if (ShouldSkipValue(fieldValue))
            {
                continue;
            }

            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, fieldValue);
            writtenNames.Add(field.Name);
        }

        foreach (PropertyInfo property in GetThemableProperties(drawableType))
        {
            if (writtenNames.Contains(property.Name))
            {
                continue;
            }

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(drawable);
            }
            catch
            {
                continue;
            }

            if (ShouldSkipValue(propertyValue))
            {
                continue;
            }

            writer.WritePropertyName(property.Name);
            serializer.Serialize(writer, propertyValue);
        }

        if (drawable is UIGroup group)
        {
            writer.WritePropertyName(nameof(UIGroup.children));
            writer.WriteStartArray();
            foreach (UIDrawable child in group.children)
            {
                if (child.dontSerialize)
                {
                    continue;
                }

                serializer.Serialize(writer, child);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void FilterNonThemableProperties(JObject jObject, Type targetType)
    {
        HashSet<string> allowedNames = GetDeserializableNames(targetType);
        foreach (JProperty property in jObject.Properties().ToList())
        {
            if (!allowedNames.Contains(property.Name))
            {
                property.Remove();
            }
        }
    }

    private static HashSet<string> GetDeserializableNames(Type type)
    {
        return DeserializableNamesCache.GetOrAdd(type, static targetType =>
        {
            HashSet<string> names = new(StringComparer.Ordinal)
            {
                "type",
                nameof(UIDrawable.id),
                nameof(UIGroup.children)
            };

            foreach (FieldInfo field in GetThemableFields(targetType))
            {
                names.Add(field.Name);
            }

            foreach (PropertyInfo property in GetThemableProperties(targetType))
            {
                if (property.CanWrite)
                {
                    names.Add(property.Name);
                }
            }

            return names;
        });
    }

    private static List<FieldInfo> GetThemableFields(Type type)
    {
        return ThemableFieldsCache.GetOrAdd(type, static targetType =>
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            return targetType
                .GetFields(flags)
                .Where(field =>
                    !field.IsStatic &&
                    field.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                    field.GetCustomAttribute<ThemableAttribute>() != null &&
                    !IsDrawableReferenceType(field.FieldType))
                .ToList();
        });
    }

    private static List<PropertyInfo> GetThemableProperties(Type type)
    {
        return ThemablePropertiesCache.GetOrAdd(type, static targetType =>
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            return targetType
                .GetProperties(flags)
                .Where(property =>
                    property.CanRead &&
                    property.GetIndexParameters().Length == 0 &&
                    property.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                    property.GetCustomAttribute<ThemableAttribute>() != null &&
                    !IsDrawableReferenceType(property.PropertyType))
                .ToList();
        });
    }

    private static bool ShouldSkipValue(object? value)
    {
        if (value == null)
        {
            return false;
        }

        return value switch
        {
            UIDrawable => true,
            IEnumerable<UIDrawable> => true,
            Delegate => true,
            IntPtr => true,
            UIntPtr => true,
            _ => false
        };
    }

    private static bool IsDrawableReferenceType(Type type)
    {
        if (typeof(UIDrawable).IsAssignableFrom(type))
        {
            return true;
        }

        if (type == typeof(string))
        {
            return false;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            Type? elementType = type.GetElementType();
            return elementType != null && typeof(UIDrawable).IsAssignableFrom(elementType);
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        Type[] genericArgs = type.GetGenericArguments();
        return genericArgs.Length == 1 && typeof(UIDrawable).IsAssignableFrom(genericArgs[0]);
    }

    private static object CreateDeserializationInstance(Type targetType)
    {
        using IDisposable _ = DrawableTracker.SuppressRegistration();

        try
        {
            object? instance = Activator.CreateInstance(targetType, nonPublic: true);
            if (instance != null)
            {
                return instance;
            }
        }
        catch (MissingMethodException)
        {
            // Fallback for drawables without a parameterless constructor.
        }

        return RuntimeHelpers.GetUninitializedObject(targetType);
    }
}
