using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania.UI.Config;

internal sealed class ScrollingLog : UIGroup
{
    private readonly UIText[] lines;
    private readonly (string text, Color4 color)[] entries;
    private int count;

    public ScrollingLog(int visibleLines, float fontSize = 16f, float spacing = 22f) : base("ScrollingLog")
    {
        lines = new UIText[visibleLines];
        entries = new (string, Color4)[visibleLines];

        for (int i = 0; i < visibleLines; i++)
        {
            UIText line = AddChild(new UIText("", fontSize));
            line.position = new Vector3(0, i * spacing, 0);
            line.isVisible = false;
            lines[i] = line;
        }
    }

    public void Clear()
    {
        count = 0;
        foreach (UIText line in lines)
        {
            line.isVisible = false;
            line.SetText("");
        }
    }

    public void Add(string text, Color4 color)
    {
        // newest at the top: shift existing entries down one slot (dropping the oldest if full)
        count = Math.Min(count + 1, entries.Length);
        for (int i = count - 1; i > 0; i--) entries[i] = entries[i - 1];

        entries[0] = (text, color);
        Render();
    }

    private void Render()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            bool used = i < count;
            lines[i].isVisible = used;
            if (used)
            {
                lines[i].SetText(entries[i].text);
                lines[i].fillColor = entries[i].color;
            }
            lines[i].MarkDirty();
        }
    }
}
