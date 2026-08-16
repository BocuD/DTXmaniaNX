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

    /// <summary>How many window pixels one canvas pixel is drawn at.</summary>
    public static float scale => CDTXMania.renderScale;

    /// <summary>Top-left of the scaled canvas within the window.</summary>
    public static Vector2 origin => (windowSize - logicalSize * CDTXMania.renderScale) / 2f;

    /// <summary>Canvas space to window space, for the legacy draws that do not go through the UI tree.</summary>
    public static Matrix4x4 toWindow =>
        Matrix4x4.CreateScale(CDTXMania.renderScale) * Matrix4x4.CreateTranslation(origin.X, origin.Y, 0f);

    /// <summary>The scale the canvas is drawn at to fill an area of this size. Whichever axis runs out
    /// first decides, so the whole canvas is always visible.</summary>
    public static float ScaleFor(Vector2 pixels) =>
        MathF.Min(pixels.X / GameWindowSize.Width, pixels.Y / GameWindowSize.Height);

    public static void SetWindowSize(Vector2 pixels)
    {
        windowSize = pixels;
        CDTXMania.renderScale = ScaleFor(pixels);
    }

    /// <summary>Centres <paramref name="root"/>'s canvas in the window at the current scale.</summary>
    public static void Place(UIGroup root)
    {
        float scale = CDTXMania.renderScale;
        root.anchor = new Vector2(0.5f, 0.5f);
        root.size = logicalSize;
        root.scale = new Vector3(scale, scale, 1f);
        root.position = new Vector3(center, 0f);
    }
}
