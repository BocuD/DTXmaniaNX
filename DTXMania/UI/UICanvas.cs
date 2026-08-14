using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania.UI;

public static class UICanvas
{
    public static Vector2 windowSize = new(GameWindowSize.Width, GameWindowSize.Height);

    public static Vector2 logicalSize => new(GameWindowSize.Width, GameWindowSize.Height);

    /// <summary>The middle of the window, which is where the middle of the canvas is placed.</summary>
    public static Vector2 center => windowSize / 2f;

    public static void SetWindowSize(Vector2 pixels)
    {
        windowSize = pixels;

        CDTXMania.renderScale = pixels.X / GameWindowSize.Width;
    }

    /// <summary>Centres <paramref name="root"/>'s canvas in the window at the current scale.</summary>
    public static void Place(UIGroup root)
    {
        root.anchor = new Vector2(0.5f, 0.5f);
        root.size = logicalSize;
        root.scale = new Vector3(CDTXMania.renderScale, CDTXMania.renderScale, 1f);
        root.position = new Vector3(center, 0f);
    }
}
