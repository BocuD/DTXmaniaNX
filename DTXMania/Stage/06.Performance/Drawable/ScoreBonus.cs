using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania;

public class ScoreBonus : UIDrawable
{
    [AddChildMenu("Game/Performance/Score Bonus")]
    public static UIDrawable Create() => new ScoreBonus();

    [Themable] public SkinResource image = SkinResource.System(@"Graphics\7_Bonus.png");
    [Themable] public int durationMs = 1020;
    //how far the image rises over its duration, in whole pixels
    [Themable] public float riseBy;

    private long shownAt = -1;
    private BaseTexture? art;
    private SkinResource loadedImage;

    public ScoreBonus()
    {
        name = "Bonus";
    }

    public void Show()
    {
        shownAt = CDTXMania.Timer.nCurrentTime;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (shownAt < 0)
        {
            return;
        }

        long elapsed = CDTXMania.Timer.nCurrentTime - shownAt;
        if (elapsed < 0 || elapsed >= durationMs)
        {
            shownAt = -1;
            return;
        }

        if (image != loadedImage)
        {
            art?.Dispose();
            art = image.IsEmpty ? null : BaseTexture.LoadFromPath(image.Resolve(ResourceType.Image));
            loadedImage = image;
        }

        if (art == null || !CDTXMania.ConfigIni.bShowScore)
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 rise = Matrix4x4.CreateTranslation(0, -MathF.Floor(riseBy * elapsed / durationMs), 0);
        art.tDraw2DMatrix(rise * localTransformMatrix * parentMatrix, new Vector2(art.Width, art.Height),
            new RectangleF(0, 0, art.Width, art.Height), UITint.Current);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Bonus"))
        {
            ResourceEditor.Draw("Image", ResourceType.Image, image, chosen => image = chosen);
            ImGui.InputInt("Duration (ms)", ref durationMs);
            ImGui.InputFloat("Rise By", ref riseBy);
            ImGui.SameLine();
            if (ImGui.Button("Show"))
            {
                Show();
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        art?.Dispose();
        art = null;
    }
}
