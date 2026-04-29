using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;
using DTXMania.UI.Inspector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Drawable.Serialization;

public class UIDrawableConverter : JsonConverter
{
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

        FilterUnsupportedProperties(jObject, targetType);

        // Construct instance first to keep non-themable default values intact.
        object result = CreateDeserializationInstance(targetType);
        serializer.Populate(jObject.CreateReader(), result);

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
        HashSet<string> writtenNames = new(StringComparer.Ordinal)
        {
            "type",
            nameof(UIDrawable.id),
            nameof(UIGroup.children)
        };

        foreach (FieldInfo field in drawableType.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (TryGetFieldSkipReason(field, out string fieldSkipReason))
            {
                LogSerializationDecision($"[SkinSerialize] Skip write field {drawableType.Name}.{field.Name}: {fieldSkipReason}");
                continue;
            }

            object? fieldValue = field.GetValue(drawable);
            if (TryGetValueSkipReason(fieldValue, out string valueSkipReason))
            {
                LogSerializationDecision($"[SkinSerialize] Skip write field value {drawableType.Name}.{field.Name}: {valueSkipReason}");
                continue;
            }

            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, fieldValue);
            writtenNames.Add(field.Name);
        }

        foreach (PropertyInfo property in drawableType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (writtenNames.Contains(property.Name))
            {
                continue;
            }

            if (TryGetPropertySkipReason(property, out string propertySkipReason))
            {
                LogSerializationDecision($"[SkinSerialize] Skip write property {drawableType.Name}.{property.Name}: {propertySkipReason}");
                continue;
            }

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(drawable);
            }
            catch
            {
                LogSerializationDecision($"[SkinSerialize] Skip write property {drawableType.Name}.{property.Name}: getter threw exception");
                continue;
            }

            if (TryGetValueSkipReason(propertyValue, out string valueSkipReason))
            {
                LogSerializationDecision($"[SkinSerialize] Skip write property value {drawableType.Name}.{property.Name}: {valueSkipReason}");
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
                    LogSerializationDecision($"[SkinSerialize] Skip write child {child.GetType().Name} '{child.name}': dontSerialize=true");
                    continue;
                }

                serializer.Serialize(writer, child);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static bool TryGetFieldSkipReason(FieldInfo field, out string reason)
    {
        if (field.IsStatic)
        {
            reason = "static field";
            return true;
        }

        if (field.Name == nameof(UIGroup.children))
        {
            reason = "children are serialized explicitly on UIGroup";
            return true;
        }

        if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null ||
            field.GetCustomAttribute<SkinNonSerializedAttribute>() != null ||
            field.GetCustomAttribute<NonSerializedAttribute>() != null)
        {
            reason = "explicit non-serialized attribute";
            return true;
        }

        if (!IsSafeSerializableType(field.FieldType) && !HasSkinSerializeOverride(field, field.FieldType))
        {
            reason = $"unsafe type '{field.FieldType.FullName}' without [SkinSerialize] override";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryGetPropertySkipReason(PropertyInfo property, out string reason)
    {
        if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
        {
            reason = "property must be readable/writable and non-indexer";
            return true;
        }

        if (property.Name is "type" or nameof(UIDrawable.parent))
        {
            reason = "runtime metadata/reference property";
            return true;
        }

        if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null ||
            property.GetCustomAttribute<SkinNonSerializedAttribute>() != null)
        {
            reason = "explicit non-serialized attribute";
            return true;
        }

        if (!IsSafeSerializableType(property.PropertyType) && !HasSkinSerializeOverride(property, property.PropertyType))
        {
            reason = $"unsafe type '{property.PropertyType.FullName}' without [SkinSerialize] override";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryGetValueSkipReason(object? value, out string reason)
    {
        if (value == null)
        {
            reason = string.Empty;
            return false;
        }

        switch (value)
        {
            case UIDrawable:
                reason = "direct UIDrawable reference";
                return true;
            case IEnumerable<UIDrawable>:
                reason = "UIDrawable collection reference";
                return true;
            case Delegate:
                reason = "delegate";
                return true;
            case IntPtr:
                reason = "IntPtr";
                return true;
            case UIntPtr:
                reason = "UIntPtr";
                return true;
            default:
                reason = string.Empty;
                return false;
        }
    }

    private static void FilterUnsupportedProperties(JObject jObject, Type drawableType)
    {
        HashSet<string> allowed = new(StringComparer.Ordinal)
        {
            "type",
            nameof(UIDrawable.id),
            nameof(UIGroup.children)
        };

        foreach (FieldInfo field in drawableType.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!TryGetFieldSkipReason(field, out _))
            {
                allowed.Add(field.Name);
            }
        }

        foreach (PropertyInfo property in drawableType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!TryGetPropertySkipReason(property, out _))
            {
                allowed.Add(property.Name);
            }
        }

        Dictionary<string, FieldInfo> fieldsByName = drawableType
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(field => field.Name, StringComparer.Ordinal);

        Dictionary<string, PropertyInfo> propertiesByName = drawableType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);

        foreach (JProperty property in jObject.Properties().ToList())
        {
            if (!allowed.Contains(property.Name))
            {
                string reason = "unknown member";
                if (fieldsByName.TryGetValue(property.Name, out FieldInfo? field) && TryGetFieldSkipReason(field, out string fieldReason))
                {
                    reason = fieldReason;
                }
                else if (propertiesByName.TryGetValue(property.Name, out PropertyInfo? reflectedProperty) && TryGetPropertySkipReason(reflectedProperty, out string propertyReason))
                {
                    reason = propertyReason;
                }

                LogSerializationDecision($"[SkinSerialize] Drop read property {drawableType.Name}.{property.Name}: {reason}");
                property.Remove();
            }
        }
    }

    private static bool IsSafeSerializableType(Type type)
    {
        if (IsDrawableReferenceType(type))
        {
            return false;
        }

        if (typeof(Delegate).IsAssignableFrom(type) || type == typeof(IntPtr) || type == typeof(UIntPtr))
        {
            return false;
        }

        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType.IsEnum || actualType.IsPrimitive)
        {
            return true;
        }

        if (actualType == typeof(string) ||
            actualType == typeof(decimal) ||
            actualType == typeof(Guid) ||
            actualType == typeof(DateTime) ||
            actualType == typeof(DateTimeOffset) ||
            actualType == typeof(TimeSpan))
        {
            return true;
        }

        if (actualType.IsValueType)
        {
            return true;
        }

        if (actualType.IsArray)
        {
            Type? elementType = actualType.GetElementType();
            return elementType != null && IsSafeSerializableType(elementType);
        }

        if (actualType.IsGenericType)
        {
            Type genericDefinition = actualType.GetGenericTypeDefinition();
            if (typeof(IDictionary).IsAssignableFrom(actualType) || genericDefinition == typeof(Dictionary<,>))
            {
                Type[] args = actualType.GetGenericArguments();
                return args.Length == 2 && IsSafeSerializableType(args[0]) && IsSafeSerializableType(args[1]);
            }

            if (typeof(IEnumerable).IsAssignableFrom(actualType))
            {
                Type[] args = actualType.GetGenericArguments();
                return args.Length == 1 && IsSafeSerializableType(args[0]);
            }
        }

        return false;
    }

    private static bool HasSkinSerializeOverride(MemberInfo member, Type type)
    {
        return member.GetCustomAttribute<SkinSerializeAttribute>() != null ||
               type.GetCustomAttribute<SkinSerializeAttribute>() != null;
    }

    private static bool IsDrawableReferenceType(Type type)
    {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (typeof(UIDrawable).IsAssignableFrom(actualType))
        {
            return true;
        }

        if (actualType == typeof(string))
        {
            return false;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(actualType))
        {
            return false;
        }

        if (actualType.IsArray)
        {
            Type? elementType = actualType.GetElementType();
            return elementType != null && typeof(UIDrawable).IsAssignableFrom(elementType);
        }

        if (!actualType.IsGenericType)
        {
            return false;
        }

        Type[] genericArgs = actualType.GetGenericArguments();
        return genericArgs.Length == 1 && typeof(UIDrawable).IsAssignableFrom(genericArgs[0]);
    }

    private static void LogSerializationDecision(string message)
    {
        if (!GameStatus.logThemeApplyDetails)
        {
            return;
        }

        Trace.TraceInformation(message);
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
