using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Renders immediate-mode drawing through a stage-registered callback, referenced by key from
/// <see cref="CStage.drawSources"/>. Because only the key is stored, it serializes like any other element,
/// so a skin can reorder or hide it. This is the serializable replacement for
/// <see cref="LegacyDrawable"/>, which existing usages can migrate to one at a time.
///
/// The callback draws with its own matrices, so render order and visibility are honoured but this
/// element's position/scale are not — that needs the callback to consume the element's transform.
/// </summary>
public class CustomDrawable : UIDrawable
{
    [Themable] public string drawKey = string.Empty;

    [JsonIgnore] private Action? _resolved;
    [JsonIgnore] private string? _resolvedKey;

    [AddChildMenu]
    public static UIDrawable Create() => new CustomDrawable();

    public CustomDrawable()
    {
    }

    public CustomDrawable(string drawKey)
    {
        this.drawKey = drawKey;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        //re-resolve only when the key changes, e.g. edited in the inspector
        if (_resolved == null || _resolvedKey != drawKey)
        {
            _resolvedKey = drawKey;
            _resolved = ResolveDrawAction();
        }

        _resolved?.Invoke();
    }

    private Action? ResolveDrawAction()
    {
        CStage? stage = CDTXMania.StageManager.rCurrentStage;
        if (stage != null && !string.IsNullOrEmpty(drawKey)
            && stage.drawSources.TryGetValue(drawKey, out Action? action))
        {
            return action;
        }

        return null;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Custom Drawable"))
        {
            return;
        }

        string[] sources = CDTXMania.StageManager.rCurrentStage.drawSources.Keys.Prepend("(none)").ToArray();
        int index = string.IsNullOrEmpty(drawKey) ? 0 : Array.IndexOf(sources, drawKey);
        if (index < 0) index = 0;
        if (ImGui.Combo("Draw Source", ref index, sources, sources.Length))
        {
            drawKey = index == 0 ? string.Empty : sources[index];
            _resolved = null;
        }
    }
}
