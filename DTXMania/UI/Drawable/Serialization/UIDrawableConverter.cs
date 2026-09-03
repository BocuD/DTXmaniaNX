using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;
using DTXMania.UI.Inspector;
using DTXMania.UI.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTXMania.UI.Drawable.Serialization;

public class UIDrawableConverter : JsonConverter
{
    //omits members still holding a freshly-constructed instance's value, so exported layout json only
    //contains what actually differs from the defaults
    private readonly bool compact;

    public UIDrawableConverter()
    {
    }

    public UIDrawableConverter(bool compact)
    {
        this.compact = compact;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(UIDrawable).IsAssignableFrom(objectType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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

        FilterUnsupportedProperties(jObject, targetType);

        // Construct instance first to keep non-themable default values intact.
        object result = CreateDeserializationInstance(targetType);
        serializer.Populate(jObject.CreateReader(), result);

        //children arrive as a plain list and so have no idea who owns them. Drawing passes matrices down
        //and does not care, but everything that walks up — the gizmos, a binding resolving its context —
        //stops dead at the first child that was loaded rather than added
        if (result is UIGroup group)
        {
            foreach (UIDrawable child in group.children)
            {
                child.SetParent(group, updateGroup: false);
            }
        }

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

        Type drawableType = drawable.GetType();
        UIDrawable? defaults = compact ? GetDefaultInstance(drawableType) : null;
        HashSet<string> writtenNames = new(StringComparer.Ordinal)
        {
            "type",
            nameof(UIGroup.children)
        };

        foreach (FieldInfo field in drawableType.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (writtenNames.Contains(field.Name))
            {
                continue; //already written explicitly, e.g. the id field
            }

            if (!drawable.ShouldSerializeMember(field.Name))
            {
                continue;
            }

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

            if (defaults != null && ValuesEqual(fieldValue, field.GetValue(defaults)))
            {
                continue; //unchanged from the default
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

            if (!drawable.ShouldSerializeMember(property.Name))
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

            if (defaults != null && ValuesEqual(propertyValue, TryGetMemberValue(property, defaults)))
            {
                continue; //unchanged from the default
            }

            writer.WritePropertyName(property.Name);
            serializer.Serialize(writer, propertyValue);
        }

        //a component instance's children come from its component file, not the layout
        if (drawable is UIGroup group && drawable is not ComponentInstance)
        {
            bool hasSerializableChild = group.children.Any(child => !child.dontSerialize);
            if (!compact || hasSerializableChild)
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

    //"anchor" was what the pivot used to be called, and nothing claims the name now, so a layout written
    //before the rename still reads correctly
    private static void RenameLegacyProperties(JObject jObject)
    {
        if (jObject.Property("anchor") is not { } legacy || jObject.Property(nameof(UIDrawable.pivot)) != null)
        {
            return;
        }

        jObject.Add(nameof(UIDrawable.pivot), legacy.Value);
        legacy.Remove();
    }

    private static void FilterUnsupportedProperties(JObject jObject, Type drawableType)
    {
        RenameLegacyProperties(jObject);

        HashSet<string> allowed = new(StringComparer.Ordinal)
        {
            "type",
            nameof(UIGroup.children),
            nameof(UiTextParameters)
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
        if (ReferencesDrawables(type))
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

    private static bool ReferencesDrawables(Type type)
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
        if (!SkinEditorWindow.logThemeApplyDetails)
        {
            return;
        }

        Trace.TraceInformation(message);
    }


    //one default-valued instance per drawable type, to compare members against for compact writes
    private static readonly Dictionary<Type, UIDrawable?> DefaultInstanceCache = new();

    private static UIDrawable? GetDefaultInstance(Type type)
    {
        if (DefaultInstanceCache.TryGetValue(type, out UIDrawable? cached))
        {
            return cached;
        }

        UIDrawable? instance = null;
        try
        {
            using IDisposable _ = DrawableTracker.SuppressRegistration();
            instance = Activator.CreateInstance(type, nonPublic: true) as UIDrawable;
        }
        catch
        {
            instance = null; //no parameterless ctor, so fall back to writing every member
        }

        DefaultInstanceCache[type] = instance;
        return instance;
    }

    private static object? TryGetMemberValue(PropertyInfo property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch
        {
            return null;
        }
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null)
        {
            return b == null;
        }

        //collections compare by reference, so an untouched list would be written to every element
        if (a is ICollection collection && collection.Count == 0)
        {
            return b is ICollection other && other.Count == 0;
        }

        return a.Equals(b);
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

        //an uninitialized instance has skipped every field initializer, so anything the type expects to
        //always exist is null and only fails later, somewhere else. Say so here instead
        Trace.TraceError($"{targetType.Name} has no parameterless constructor, so it deserializes " +
                         "uninitialized. Add one, or mark the element dontSerialize.");

        return RuntimeHelpers.GetUninitializedObject(targetType);
    }
}
