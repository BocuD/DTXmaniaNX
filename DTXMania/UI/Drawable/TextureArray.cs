using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// Draws one of a set of textures, and plays through them on its own at <see cref="frameRate"/>. Setting
/// <see cref="textureIndex"/> — from code, or from an animation clip — moves playback there and it carries
/// on from that frame, so an effect only has to say where to start rather than key every frame.
/// </summary>
public class TextureArray : UITexture
{
    //the frame on screen; whatever sets this decides where playback carries on from
    [Themable] public int textureIndex;

    //frames per second; zero holds the frame, leaving the index entirely to whoever sets it
    [Themable] public float frameRate;

    //the frame to come back to after the last one, so an intro can run once and the tail loop forever
    [Themable] public int loopStart;

    public BaseTexture[] textures = [];

    public RectangleF clipRect;

    private long lastDrawTime;
    private float frameTime;
    private int playedIndex = -1;

    public TextureArray(BaseTexture[] textures) : base(textures[0])
    {
        this.textures = textures;

        clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible)
        {
            return;
        }

        Advance();

        if (textures.Length <= textureIndex)
        {
            Trace.TraceWarning($"TextureArray: textureIndex {textureIndex} is out of bounds for textures array of length {textures.Length}");
            return;
        }

        var target = textures[textureIndex];

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;
        target.tDraw2DMatrix(combinedMatrix, size, clipRect, color);
    }

    private void Advance()
    {
        long now = CDTXMania.Timer.nCurrentTime;

        //a frame that was hidden, or the first one, has nothing meaningful to measure against
        float elapsed = lastDrawTime == 0 ? 0.0f : Math.Min((now - lastDrawTime) / 1000.0f, 0.25f);
        lastDrawTime = now;

        if (frameRate <= 0.0f || textures.Length == 0)
        {
            return;
        }

        //an index set from outside starts that frame afresh rather than inheriting the old one's remainder
        if (textureIndex != playedIndex)
        {
            frameTime = 0.0f;
        }

        frameTime += elapsed;
        float frameLength = 1.0f / frameRate;

        while (frameTime >= frameLength)
        {
            frameTime -= frameLength;
            textureIndex = textureIndex + 1 < textures.Length ? textureIndex + 1 : loopStart;
        }

        playedIndex = textureIndex;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("TextureArray"))
        {
            ImGui.SliderInt("Texture Index", ref textureIndex, 0, textures.Length - 1);
            ImGui.InputFloat("Frame Rate", ref frameRate);
            ImGui.InputInt("Loop Start", ref loopStart);
        }
    }
}
