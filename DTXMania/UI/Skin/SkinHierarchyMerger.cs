using DTXMania.UI.Drawable;
using DTXMania.UI.Drawable.Serialization;
using DTXMania.UI.Inspector;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace DTXMania.UI.Skin;

internal static class SkinHierarchyMerger
{
    internal static void ApplySkin(UIGroup targetGroup, UIGroup? loadedSkin)
    {
        if (loadedSkin == null)
        {
            return;
        }

        if (GameStatus.logThemeApplyDetails)
        {
            Trace.TraceInformation($"[ThemeApply] Applying {loadedSkin.GetType().Name} '{loadedSkin.name}' -> {targetGroup.GetType().Name} '{targetGroup.name}'");
        }

        CopySkinStateFrom(targetGroup, loadedSkin);
        InvokePostApplyDeserialize(targetGroup);

        HashSet<UIDrawable> matchedLoadedChildren = [];
        HashSet<UIDrawable> matchedTargetChildren = [];

        // Pass 1: deterministic match by (name + concrete type).
        foreach (UIDrawable targetChild in targetGroup.children)
        {
            if (string.IsNullOrWhiteSpace(targetChild.name))
            {
                continue;
            }

            UIDrawable? loadedChild = loadedSkin.children.FirstOrDefault(child =>
                !matchedLoadedChildren.Contains(child) &&
                !string.IsNullOrWhiteSpace(child.name) &&
                child.name == targetChild.name &&
                child.GetType() == targetChild.GetType());

            if (loadedChild == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] No named match for {targetChild.GetType().Name} '{targetChild.name}'");
                }
                continue;
            }

            matchedLoadedChildren.Add(loadedChild);
            matchedTargetChildren.Add(targetChild);
            ApplyMatchedChild(targetChild, loadedChild);
        }

        // Pass 2: fallback for existing children whose names changed, matched by (type + order).
        foreach (UIDrawable targetChild in targetGroup.children)
        {
            if (matchedTargetChildren.Contains(targetChild))
            {
                continue;
            }

            UIDrawable? loadedChild = loadedSkin.children.FirstOrDefault(child =>
                !matchedLoadedChildren.Contains(child) &&
                child.GetType() == targetChild.GetType());

            if (loadedChild == null)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] No type-order fallback match for {targetChild.GetType().Name} '{targetChild.name}' (target group '{targetGroup.name}')");
                }
                continue;
            }

            if (GameStatus.logThemeApplyDetails)
            {
                Trace.TraceInformation($"[ThemeApply] Using type-order fallback match {targetChild.GetType().Name}: target '{targetChild.name}' <- loaded '{loadedChild.name}'");
            }

            matchedLoadedChildren.Add(loadedChild);
            matchedTargetChildren.Add(targetChild);
            ApplyMatchedChild(targetChild, loadedChild);
        }

        foreach (UIDrawable loadedChild in loadedSkin.children)
        {
            if (matchedLoadedChildren.Contains(loadedChild))
            {
                continue;
            }

            bool duplicateByIdentity = targetGroup.children.Any(existing =>
                existing.GetType() == loadedChild.GetType() &&
                string.Equals(existing.name, loadedChild.name, StringComparison.Ordinal));

            if (duplicateByIdentity)
            {
                if (GameStatus.logThemeApplyDetails)
                {
                    Trace.TraceInformation($"[ThemeApply] Skip adding duplicate child {loadedChild.GetType().Name} '{loadedChild.name}' (already exists in target group '{targetGroup.name}')");
                }
                continue;
            }

            UIDrawable? clonedChild = CloneDrawable(loadedChild);
            if (clonedChild == null)
            {
                Trace.TraceError($"[ThemeApply] Failed to instantiate missing child {loadedChild.GetType().Name} '{loadedChild.name}'");
                continue;
            }

            targetGroup.AddChild(clonedChild);
            if (GameStatus.logThemeApplyDetails)
            {
                Trace.TraceInformation($"[ThemeApply] Added new child {clonedChild.GetType().Name} '{clonedChild.name}' from skin");
            }
        }
    }

    private static void ApplyMatchedChild(UIDrawable targetChild, UIDrawable loadedChild)
    {
        if (targetChild is UIGroup targetGroup && loadedChild is UIGroup loadedGroup)
        {
            ApplySkin(targetGroup, loadedGroup);
        }
        else
        {
            CopySkinStateFrom(targetChild, loadedChild);
            InvokePostApplyDeserialize(targetChild);
        }
    }

    private static void InvokePostApplyDeserialize(UIDrawable targetDrawable)
    {
        try
        {
            targetDrawable.OnDeserialize();
        }
        catch (Exception e)
        {
            Trace.TraceError($"[ThemeApply] Failed post-apply OnDeserialize for {targetDrawable.GetType().Name} '{targetDrawable.name}': {e.Message}");
        }
    }

    private static UIDrawable? CloneDrawable(UIDrawable drawable)
    {
        try
        {
            JsonSerializerSettings settings = new()
            {
                Converters = [new UIDrawableConverter()]
            };

            string json = JsonConvert.SerializeObject(drawable, settings);
            return SkinHierarchySerializer.DeserializeDrawableFromJson(json, invokeDeserializeCallbacks: true);
        }
        catch (Exception e)
        {
            Trace.TraceError($"[ThemeApply] Error cloning drawable {drawable.GetType().Name} '{drawable.name}': {e.Message}");
            return null;
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
