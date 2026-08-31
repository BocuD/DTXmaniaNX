using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;

namespace DTXMania.UI;

public enum UiCanvasFit
{
    Letterbox,
    Fill
}

public static class UICanvas
{
    public static Vector2 windowSize = new(GameWindowSize.Width, GameWindowSize.Height);

    public static Vector2 logicalSize => new(GameWindowSize.Width, GameWindowSize.Height);

    public static Vector2 canvasSize => CDTXMania.renderScale <= 0f
        ? logicalSize
        : windowSize / CDTXMania.renderScale;

    /// <summary>The middle of the window, which is where the middle of the canvas is placed.</summary>
    public static Vector2 center => windowSize / 2f;

    public static readonly Vector2 Center = new(0.5f, 0.5f);
    public static readonly Vector2 TopRight = new(1f, 0f);
    public static readonly Vector2 Right = new(1f, 0.5f);
    public static readonly Vector2 BottomLeft = new(0f, 1f);

    /// <summary>A design space point as an offset from where an anchor puts the origin, so a layout is
    /// still written in the coordinates it was designed in.</summary>
    public static Vector3 FromAnchor(Vector2 anchor, float x, float y)
        => new(x - anchor.X * logicalSize.X, y - anchor.Y * logicalSize.Y, 0f);

    public static Vector3 FromCenter(float x, float y) => FromAnchor(Center, x, y);

    /// <summary>How many window pixels one canvas pixel is drawn at.</summary>
    public static float scale => CDTXMania.renderScale;

    //the legacy draws never go through the tree, so they have to be told what the stage root did
    private static UiCanvasFit stageFit = UiCanvasFit.Letterbox;

    /// <summary>Top-left of the scaled canvas within the window. A filling canvas starts at the window's
    /// own corner, which is where its tree puts an unanchored child.</summary>
    public static Vector2 origin => stageFit == UiCanvasFit.Fill
        ? Vector2.Zero
        : (windowSize - logicalSize * CDTXMania.renderScale) / 2f;

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
    public static void Place(UIGroup root, UiCanvasFit? overrideFit = null)
    {
        float scale = CDTXMania.renderScale;
        UiCanvasFit fit = overrideFit ?? (root as StageRoot)?.canvasFit ?? UiCanvasFit.Letterbox;

        //the persistent group is placed too, but only the stage decides where the legacy draws go
        if (root is StageRoot)
        {
            stageFit = fit;
        }

        root.pivot = new Vector2(0.5f, 0.5f);
        root.size = fit == UiCanvasFit.Fill ? canvasSize : logicalSize;
        root.scale = new Vector3(scale, scale, 1f);
        root.position = new Vector3(center, 0f);
    }
}
