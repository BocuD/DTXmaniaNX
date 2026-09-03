using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public static class AspectRatioPicker
{
    private static readonly (string label, float width)[] options =
    [
        ("4:3  (960)", 960f),
        ("16:10  (1152)", 1152f),
        ("16:9  (1280)", 1280f),
        ("21:9  (1680)", 1680f)
    ];

    public static void Draw(string label, ref float width)
    {
        float current = width;
        int index = Array.FindIndex(options, option => option.width == current);

        ImGui.SetNextItemWidth(150);
        if (!ImGui.BeginCombo(label, index >= 0 ? options[index].label : $"{current:0} wide"))
        {
            return;
        }

        foreach ((string optionLabel, float optionWidth) in options)
        {
            if (ImGui.Selectable(optionLabel, optionWidth == current))
            {
                width = optionWidth;
            }
        }

        ImGui.EndCombo();
    }
}
