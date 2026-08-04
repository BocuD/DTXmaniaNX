using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Skin;
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

    //System-relative paths for a layout that names its frames instead of being handed textures, e.g. the
    //three panel arts a settings row picks between
    [Themable] [SkinSerialize] public List<string> resources = [];

    [Themable] public RectangleF clipRect;

    private long lastDrawTime;
    private float frameTime;
    private int playedIndex = -1;

    private bool ownsTextures;

    public TextureArray() : base(BaseTexture.None)
    {
    }

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

        if (textures.Length != resources.Count && resources.Count > 0)
        {
            LoadResources();
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
        target.tDraw2DMatrix(combinedMatrix, size, ClipFor(target), color);
    }

    //frames can differ in size, so an unset extent follows whichever one is on screen; only what the
    //layout actually asked for is fixed
    private RectangleF ClipFor(BaseTexture frame)
    {
        return new RectangleF(
            clipRect.X,
            clipRect.Y,
            clipRect.Width > 0.0f ? clipRect.Width : frame.Width - clipRect.X,
            clipRect.Height > 0.0f ? clipRect.Height : frame.Height - clipRect.Y);
    }

    //a frame that fails to load stays empty rather than shifting every index after it
    private void LoadResources()
    {
        textures = new BaseTexture[resources.Count];
        ownsTextures = true;

        for (int i = 0; i < resources.Count; i++)
        {
            string fullPath = SkinManager.SystemPath(resources[i]);
            if (!File.Exists(fullPath))
            {
                Trace.TraceError($"TextureArray frame {fullPath} does not exist.");
                textures[i] = BaseTexture.None;
                continue;
            }

            textures[i] = BaseTexture.LoadFromPath(fullPath);
        }

        base.SetTexture(textures[0], updateSize: false);

        //a compact layout leaves size at its default, and a frame set is drawn at its own size unless the
        //layout says otherwise
        if (size.X is 0.0f or 1.0f && size.Y is 0.0f or 1.0f)
        {
            size = new Vector2(texture.Width, texture.Height);
        }
    }

    public override void Dispose()
    {
        if (ownsTextures)
        {
            foreach (BaseTexture loaded in textures)
            {
                loaded.Dispose();
            }

            textures = [];
        }

        base.Dispose();
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
