using DTXMania.Core;
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
        ImGui.SameLine();
        if (ImGui.Button("Run GC"))
        {
            CDTXMania.tRunGarbageCollector();
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
            ImGui.Text(drawable.Value.TryGetTarget(out var target) ? target.type : "null");
            ImGui.TableNextColumn();
            ImGui.Text(drawable.Value.TryGetTarget(out var target2) ? (string.IsNullOrEmpty(target2.name) ? target2.GetType().Name : target2.name) : "null");
        }
        ImGui.EndTable();
        ImGui.End();
    }

    public static UIDrawable? GetDrawable(string guid)
    {
        if (drawables.TryGetValue(guid, out WeakReference<UIDrawable>? weakReference) 
            && weakReference.TryGetTarget(out UIDrawable? drawable))
        {
            return drawable;
        }

        return null;
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
    
    //implicit conversion from T to DrawableReference<T> so we can do DrawableReference<T> drawable = new T();
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
        //get type of value
        Type type = value?.GetType() ?? throw new JsonSerializationException("Value is null");

        //check if it inherits from drawablereference
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(DrawableReference<>))
        {
            throw new JsonSerializationException("Value is not a DrawableReference");
        }

        //get the id field through reflection
        var fieldInfo = type.GetField("id");
        string id = fieldInfo.GetValue(value) as string ?? throw new JsonSerializationException("ID is null");
        
        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(type.FullName);
        writer.WritePropertyName("id");
        writer.WriteValue(id);
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject)
        {
            //create generic object of the right type
            object? drawableReference;
            string type = "";
            string id = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = reader.Value.ToString();
                    reader.Read();
                    
                    switch (propertyName)
                    {
                        case "type":
                            type = reader.Value.ToString()!;
                            break;
                        
                        case "id":
                            id = reader.Value.ToString()!;
                            break;
                    }
                }
            }

            Type? t = Type.GetType(type) ?? objectType;
            
            //create instance
            drawableReference = Activator.CreateInstance(t);
            
            //set id
            var fieldInfo = t.GetField("id");
            if (fieldInfo == null)
            {
                throw new JsonSerializationException("ID field not found");
            }
            fieldInfo.SetValue(drawableReference, id);
            
            return drawableReference;
        }

        throw new JsonSerializationException("Invalid token type for DrawableReferenceSerializer");
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DrawableReference<>);
    }
}