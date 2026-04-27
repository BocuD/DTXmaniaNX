using System.Diagnostics;
using System.Reflection;
using System.Numerics;
using DTXMania.UI.Drawable.Serialization;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public class UIGroup : UIDrawable
{
    [Themable] public bool sortByRenderOrder = true;
    public List<UIDrawable> children = [];

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIGroup("New UIGroup");
    }

    public UIGroup()
        : this("New UIGroup")
    {
    }

    public UIGroup(string name)
    {
        this.name = name;
    }

    public T AddChild<T>(T element, bool setParent = true) where T : UIDrawable
    {
        children.Add(element);
        if (setParent)
        {
            element.SetParent(this, false);
        }

        return element;
    }

    public T GetChild<T>(int i) where T : UIDrawable
    {
        return (T)children[i];
    }

    public T? GetChild<T>(string name) where T : UIDrawable
    {
        return (T?)children.FirstOrDefault(x => x.name == name);
    }

    public UIDrawable GetChild(int i)
    {
        return children[i];
    }

    public void RemoveChild(UIDrawable element)
    {
        children.Remove(element);
    }

    public void ClearChildren()
    {
        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (sortByRenderOrder)
        {
            children.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));
        }

        for (int index = 0; index < children.Count; index++)
        {
            UIDrawable element = children[index];
            if (!element.isVisible)
            {
                continue;
            }

            try
            {
                element.Draw(combinedMatrix);
            }
            catch (Exception e)
            {
                string stackTrace = e.StackTrace ?? "No stack trace";
                Trace.TraceError($"Error drawing {element.name}: {e} Stacktrace: {stackTrace}");
            }
        }
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();

        foreach (UIDrawable? child in children)
        {
            child?.SetParent(this, false);
        }

        children.RemoveAll(x => x == null);
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (UIDrawable element in children)
        {
            element.Dispose();
        }

        children.Clear();
    }

    public int GetChildIndex(UIDrawable node)
    {
        return children.IndexOf(node);
    }

    public void SetChildIndex(UIDrawable node, int index)
    {
        if (index < 0 || index >= children.Count)
        {
            Trace.TraceError($"Index {index} is out of bounds for children list of size {children.Count}");
            return;
        }

        int currentIndex = GetChildIndex(node);
        if (currentIndex != -1)
        {
            children.RemoveAt(currentIndex);
            children.Insert(index, node);
        }
    }

    public string SerializeToJSON()
    {
        JsonSerializerSettings settings = new()
        {
            Formatting = Formatting.Indented,
            Converters = [new UIDrawableConverter()]
        };

        try
        {
            return JsonConvert.SerializeObject(this, settings);
        }
        catch (Exception e)
        {
            string stackTrace = e.StackTrace ?? "No stack trace";
            Trace.TraceError($"Failed to save UIGroup: {e} Stacktrace: {stackTrace}");
            return string.Empty;
        }
    }

    public static UIGroup? DeserializeFromJSON(string json)
    {
        try
        {
            UIGroup? loadedGroup = JsonConvert.DeserializeObject<UIGroup>(json, new UIDrawableConverter());
            if (loadedGroup == null)
            {
                Trace.TraceError("Deserialization returned null, possibly due to an empty or invalid JSON.");
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

    public override void DrawInspector()
    {
        base.DrawInspector();
        ImGui.Checkbox("Sort by Render Order", ref sortByRenderOrder);
    }

    //loadedSkin is a UIGroup that was deserialized from the skin file, we need to apply its properties to the target UIGroup and its children
    internal void ApplySkin(UIGroup? loadedSkin)
    {
        if (loadedSkin == null)
        {
            return;
        }

        if (GameStatus.logThemeApplyDetails)
        {
            Trace.TraceInformation($"[ThemeApply] Applying {loadedSkin.GetType().Name} '{loadedSkin.name}' -> {GetType().Name} '{name}'");
        }

        CopySkinStateFrom(this, loadedSkin);

        foreach (UIDrawable targetChild in children)
        {
            if (string.IsNullOrWhiteSpace(targetChild.name))
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip child with empty name on target {GetType().Name} '{name}'");
                }
                continue;
            }

            UIDrawable? loadedChild = loadedSkin.children.FirstOrDefault(child =>
                !string.IsNullOrWhiteSpace(child.name) &&
                child.name == targetChild.name &&
                child.GetType() == targetChild.GetType());

            if (loadedChild == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] No matching themed child for {targetChild.GetType().Name} '{targetChild.name}'");
                }
                continue;
            }

            if (targetChild is UIGroup targetGroup && loadedChild is UIGroup loadedGroup)
            {
                targetGroup.ApplySkin(loadedGroup);
            }
            else
            {
                CopySkinStateFrom(targetChild, loadedChild);
            }
        }
    }

    private static void CopySkinStateFrom(UIDrawable targetDrawable, UIDrawable loadedDrawable)
    {
        Type loadedType = loadedDrawable.GetType();
        Type targetType = targetDrawable.GetType();

        if (targetType != loadedType)
        {
            if (GameStatus.logThemeApplyDetails)
            {
                Trace.TraceInformation($"[ThemeApply] Type mismatch skip: source {loadedType.FullName}, target {targetType.FullName}");
            }
            return;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

        foreach (FieldInfo sourceField in loadedType.GetFields(flags))
        {
            if (sourceField.IsStatic || sourceField.GetCustomAttribute<ThemableAttribute>() == null)
            {
                continue;
            }

            FieldInfo? targetField = targetType.GetField(sourceField.Name, flags);
            if (targetField == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip field {targetType.Name}.{sourceField.Name}: missing target field");
                }
                continue;
            }

            if (targetField.IsInitOnly || targetField.IsLiteral)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip field {targetType.Name}.{sourceField.Name}: readonly/const");
                }
                continue;
            }

            if (targetField.GetCustomAttribute<ThemableAttribute>() == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip field {targetType.Name}.{sourceField.Name}: target not [Themable]");
                }
                continue;
            }

            if (!targetField.FieldType.IsAssignableFrom(sourceField.FieldType))
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip field {targetType.Name}.{sourceField.Name}: incompatible types {sourceField.FieldType.Name} -> {targetField.FieldType.Name}");
                }
                continue;
            }

            try
            {
                targetField.SetValue(targetDrawable, sourceField.GetValue(loadedDrawable));
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Applied field {targetType.Name}.{sourceField.Name} on '{targetDrawable.name}'");
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to apply skin field {sourceField.Name} to {targetDrawable.name}: {e.Message}");
            }
        }

        foreach (PropertyInfo sourceProperty in loadedType.GetProperties(flags))
        {
            if (!sourceProperty.CanRead || sourceProperty.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (sourceProperty.GetCustomAttribute<ThemableAttribute>() == null)
            {
                continue;
            }

            PropertyInfo? targetProperty = targetType.GetProperty(sourceProperty.Name, flags);
            if (targetProperty == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip property {targetType.Name}.{sourceProperty.Name}: missing target property");
                }
                continue;
            }

            if (!targetProperty.CanWrite || targetProperty.GetIndexParameters().Length > 0)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip property {targetType.Name}.{sourceProperty.Name}: not writable/indexer");
                }
                continue;
            }

            if (targetProperty.GetCustomAttribute<ThemableAttribute>() == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip property {targetType.Name}.{sourceProperty.Name}: target not [Themable]");
                }
                continue;
            }

            MethodInfo? setMethod = targetProperty.GetSetMethod();
            if (setMethod == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip property {targetType.Name}.{sourceProperty.Name}: missing setter");
                }
                continue;
            }

            if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip property {targetType.Name}.{sourceProperty.Name}: incompatible types {sourceProperty.PropertyType.Name} -> {targetProperty.PropertyType.Name}");
                }
                continue;
            }

            try
            {
                targetProperty.SetValue(targetDrawable, sourceProperty.GetValue(loadedDrawable));
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Applied property {targetType.Name}.{sourceProperty.Name} on '{targetDrawable.name}'");
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to apply skin property {sourceProperty.Name} to {targetDrawable.name}: {e.Message}");
            }
        }
    }
}
