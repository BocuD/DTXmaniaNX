using System.Diagnostics;
using DTXMania.UI.Drawable;
using DTXMania.UI.Drawable.Serialization;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

public static class SkinHierarchySerializer
{
    public static string SerializeToJson(UIGroup group)
    {
        JsonSerializerSettings settings = new()
        {
            Formatting = Formatting.Indented,
            Converters = [new UIDrawableConverter()]
        };

        try
        {
            return JsonConvert.SerializeObject(group, settings);
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to save UIGroup: {e} Stacktrace: {stackTrace}");
            return string.Empty;
        }
    }

    public static UIGroup? DeserializeFromJson(string json, bool invokeDeserializeCallbacks = true)
    {
        try
        {
            UIGroup? loadedGroup = JsonConvert.DeserializeObject<UIGroup>(json, new UIDrawableConverter());
            if (loadedGroup == null)
            {
                Trace.TraceError("Deserialization returned null, possibly due to an empty or invalid JSON.");
                return null;
            }

            if (invokeDeserializeCallbacks)
            {
                InvokeDeserializeCallbacksRecursive(loadedGroup);
            }

            return loadedGroup;
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to deserialize UIGroup: {e} Stacktrace: {stackTrace}");
            return null;
        }
    }

    internal static UIDrawable? DeserializeDrawableFromJson(string json, bool invokeDeserializeCallbacks = true)
    {
        try
        {
            UIDrawable? loadedDrawable = JsonConvert.DeserializeObject<UIDrawable>(json, new UIDrawableConverter());
            if (loadedDrawable == null)
            {
                Trace.TraceError("Deserialization returned null, possibly due to an empty or invalid JSON.");
                return null;
            }

            if (invokeDeserializeCallbacks)
            {
                InvokeDeserializeCallbacksRecursive(loadedDrawable);
            }

            return loadedDrawable;
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to deserialize UIDrawable: {e} Stacktrace: {stackTrace}");
            return null;
        }
    }

    private static void InvokeDeserializeCallbacksRecursive(UIDrawable drawable)
    {
        drawable.OnDeserialize();

        if (drawable is not UIGroup group)
        {
            return;
        }

        foreach (UIDrawable child in group.children)
        {
            InvokeDeserializeCallbacksRecursive(child);
        }
    }
}
