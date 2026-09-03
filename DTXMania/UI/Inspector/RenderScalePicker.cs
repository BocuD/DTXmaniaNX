using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Picks the scale a tree is drawn at. Null follows whatever the game is drawing at.
/// </summary>
public static class RenderScalePicker
{
    //labelled with the resolution each scale means, since that is what a skin is authored against
    private static readonly (string label, float? scale)[] options =
    [
        ("Window", null),
        ("1x  (1280x720)", 1.0f),
        ("1.5x  (1920x1080)", 1.5f),
        ("2x  (2560x1440)", 2.0f),
        ("3x  (3840x2160)", 3.0f)
    ];

    public static void Draw(string label, ref float? scale)
    {
        float? current = scale;

        ImGui.SetNextItemWidth(150);
        if (!ImGui.BeginCombo(label, options.First(option => option.scale == current).label))
        {
            return;
        }

        foreach ((string optionLabel, float? optionScale) in options)
        {
            if (ImGui.Selectable(optionLabel, optionScale == current))
            {
                scale = optionScale;
            }
        }

        ImGui.EndCombo();
    }
}
