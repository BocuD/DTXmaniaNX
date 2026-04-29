using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI;

public class DrawableTracker
{
    public static Dictionary<string, WeakReference<UIDrawable>> drawables = new();
    private static int registrationSuppressionDepth;

    public static IDisposable SuppressRegistration()
    {
        registrationSuppressionDepth++;
        return new RegistrationSuppressionScope();
    }

    public static void Register(UIDrawable drawable)
    {
        if (registrationSuppressionDepth > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(drawable.id))
        {
            throw new InvalidOperationException($"Drawable id is missing for {drawable.GetType().FullName} during registration.");
        }

        drawables[drawable.id] = new WeakReference<UIDrawable>(drawable);
    }

    public static void Remove(UIDrawable drawable)
    {
        drawables.Remove(drawable.id);
    }

    public static void DrawWindow()
    {
        if (ImGui.Begin("Drawables", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text($"Count: {drawables.Count}");
            ImGui.SameLine();
            if (ImGui.Button("Run GC"))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            ImGui.BeginTable("DrawablesTable", 3);
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Name");
            ImGui.TableHeadersRow();

            foreach (var drawable in drawables)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Key);
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Value.TryGetTarget(out UIDrawable? target) ? target.type : "null");
                ImGui.TableNextColumn();
                ImGui.Text(drawable.Value.TryGetTarget(out UIDrawable? target2)
                    ? (string.IsNullOrEmpty(target2.name) ? target2.GetType().Name : target2.name)
                    : "null");
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    public static UIDrawable? GetDrawable(string guid)
    {
        if (drawables.TryGetValue(guid, out WeakReference<UIDrawable>? weakReference) &&
            weakReference.TryGetTarget(out UIDrawable? drawable))
        {
            return drawable;
        }

        return null;
    }

    private sealed class RegistrationSuppressionScope : IDisposable
    {
        public void Dispose()
        {
            registrationSuppressionDepth = Math.Max(0, registrationSuppressionDepth - 1);
        }
    }
}

[JsonConverter(typeof(DrawableReferenceSerializer))]
public class DrawableReference<T> where T : UIDrawable
{
    public string id = string.Empty;
    public WeakReference<T>? reference;

    public DrawableReference()
    {
    }

    public DrawableReference(string id)
    {
        this.id = id;
    }

    public DrawableReference(T drawable)
    {
        id = drawable.id;
        reference = new WeakReference<T>(drawable);
    }

    public static implicit operator DrawableReference<T>(T drawable)
    {
        return new DrawableReference<T>(drawable);
    }

    public static implicit operator T(DrawableReference<T> drawable)
    {
        return drawable.Get()!;
    }

    public T? Get()
    {
        T? value = null;

        if (reference != null)
        {
            reference.TryGetTarget(out T? drawable);
            if (drawable != null)
            {
                return drawable;
            }
        }

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (DrawableTracker.drawables.TryGetValue(id, out WeakReference<UIDrawable>? weakReference) &&
            weakReference.TryGetTarget(out UIDrawable? newValue))
        {
            value = newValue as T;
            if (value != null)
            {
                reference = new WeakReference<T>(value);
            }
        }

        return value;
    }
}

public class DrawableReferenceSerializer : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        Type type = value?.GetType() ?? throw new JsonSerializationException("Value is null");

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(DrawableReference<>))
        {
            throw new JsonSerializationException("Value is not a DrawableReference");
        }

        var fieldInfo = type.GetField("id");
        string id = fieldInfo?.GetValue(value) as string ?? throw new JsonSerializationException("ID is null");

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(type.FullName);
        writer.WritePropertyName("id");
        writer.WriteValue(id);
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonSerializationException("Invalid token type for DrawableReferenceSerializer");
        }

        object? drawableReference;
        string type = string.Empty;
        string id = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonToken.PropertyName)
            {
                continue;
            }

            string propertyName = reader.Value?.ToString() ?? string.Empty;
            reader.Read();

            switch (propertyName)
            {
                case "type":
                    type = reader.Value?.ToString() ?? string.Empty;
                    break;
                case "id":
                    id = reader.Value?.ToString() ?? string.Empty;
                    break;
            }
        }

        Type? t = Type.GetType(type) ?? objectType;
        drawableReference = Activator.CreateInstance(t);

        var idField = t.GetField("id");
        if (idField == null)
        {
            throw new JsonSerializationException("ID field not found");
        }

        idField.SetValue(drawableReference, id);
        return drawableReference;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DrawableReference<>);
    }
}
