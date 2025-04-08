using DTXMania.UI.Drawable;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI;

public class DrawableTracker
{
    public static Dictionary<string, WeakReference<UIDrawable>> drawables = new();
    
    public static void Register(UIDrawable drawable)
    {
        drawables[drawable.id] = new WeakReference<UIDrawable>(drawable);
    }

    public static void Remove(string id)
    {
        drawables.Remove(id);
    }

    public static void DrawWindow()
    {
        //draw imgui window with table and scroll to show objects
        ImGui.Begin("Drawables");
        
        //count
        ImGui.Text($"Count: {drawables.Count}");
        
        ImGui.BeginTable("DrawablesTable", 2);
        ImGui.TableSetupColumn("ID");
        ImGui.TableSetupColumn("Type");
        ImGui.TableHeadersRow();
        foreach (var drawable in drawables)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(drawable.Key);
            ImGui.TableNextColumn();
            ImGui.Text(drawable.Value.TryGetTarget(out var target) ? target.type : "null");
        }
        ImGui.EndTable();
        ImGui.End();
    }
}

[JsonConverter(typeof(DrawableReferenceSerializer))]
public class DrawableReference<T> where T : UIDrawable
{
    public string id = String.Empty;
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

    public T? GetDrawable()
    {
        T? value = null;
        
        //we have the weak reference, try to get the value
        if (reference != null)
        {
            reference.TryGetTarget(out T? drawable);
            if (drawable != null) return drawable;
        }
        
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        
        //if not, try to get it from the manager
        if (DrawableTracker.drawables.TryGetValue(id, out var weakReference) && weakReference.TryGetTarget(out UIDrawable? newValue))
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
        if (value is DrawableReference<UIDrawable> drawableReference)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteValue(drawableReference.id);
            writer.WriteEndObject();
        }
        else
        {
            throw new JsonSerializationException("Invalid type for DrawableReferenceSerializer");
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            DrawableReference<UIDrawable> drawableReference = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = reader.Value.ToString();
                    reader.Read();

                    if (propertyName == "id")
                    {
                        drawableReference.id = reader.Value.ToString();
                    }
                }
            }
            return drawableReference;
        }

        throw new JsonSerializationException("Invalid token type for DrawableReferenceSerializer");
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DrawableReference<UIDrawable>);
    }
}