using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.Core.Video;

public enum VideoSource
{
    Embedded,
    Linked
}

public enum VideoFit
{
    Stretch,
    Contain,
    Cover
}

public class UINewVideoRenderer : UIDrawable
{
    //only an embedded renderer plays this
    public VideoPlayerController Controller { get; } = new();

    [Themable] public VideoSource source = VideoSource.Embedded;
    [Themable] public string link = "Video.Frame";
    [Themable] public VideoFit fit = VideoFit.Stretch;

    //tint and opacity applied to the drawn frame, so callers can fade a video the way they would an image
    [Themable] public Color4 color = Color4.White;

    //with a video set it loads itself; leave it empty and call LoadVideo for a per-song one
    [Themable] public SkinResource video;

    [JsonIgnore] private string? _lastVideoLoadAttempt;

    [AddChildMenu("Video/Video Player")]
    public static UINewVideoRenderer CreateAsync()
    {
        return new UINewVideoRenderer();
    }

    [AddChildMenu("Video/Video Player (Software)")]
    public static UINewVideoRenderer CreateSoftware()
    {
        return new UINewVideoRenderer { Controller = { UseSoftwareDecoder = true } };
    }

    [AddChildMenu("Video/Linked Video")]
    public static UINewVideoRenderer CreateLinked()
    {
        return new UINewVideoRenderer { source = VideoSource.Linked, fit = VideoFit.Contain };
    }

    public UINewVideoRenderer()
    {
        //a placeholder box until a frame states the real one
        size = UISize.Auto(new Vector2(640, 480));
    }

    public bool LoadVideo(string path)
    {
        if (!CDTXMania.ConfigIni.bAVIEnabled) return false;

        if (Controller.TryLoadVideo(path) && Controller.CurrentFrame.IsValid)
        {
            size.SetContent(new Vector2(Controller.CurrentFrame.Texture.Width, Controller.CurrentFrame.Texture.Height));
            return true;
        }

        return false;
    }

    public override void OnDeserialize()
    {
        base.OnDeserialize();
        LoadDeclaredVideo();
    }

    private void LoadDeclaredVideo()
    {
        if (source != VideoSource.Embedded || video.IsEmpty)
        {
            return;
        }

        _lastVideoLoadAttempt = video.path;

        string full = video.Resolve(ResourceType.Video);
        if (!string.IsNullOrWhiteSpace(full))
        {
            LoadVideo(full);
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //once per distinct resource, so a missing file is not retried every frame
        if (source == VideoSource.Embedded && !video.IsEmpty && video.path != _lastVideoLoadAttempt)
        {
            LoadDeclaredVideo();
        }

        if (!isVisible) return;

        BaseTexture? texture = CurrentTexture();
        if (texture == null || !texture.IsValid() || texture.notFound)
        {
            return;
        }

        //a stream can change dimensions mid-playback
        size.SetContent(new Vector2(texture.Width, texture.Height));

        UpdateLocalTransformMatrix();
        DrawFitted(texture, localTransformMatrix * parentMatrix);
    }

    private BaseTexture? CurrentTexture()
    {
        if (source == VideoSource.Linked)
        {
            return this.TryResolveContextTexture(link, out BaseTexture linked) ? linked : null;
        }

        Controller.Update();
        return Controller.CurrentFrame.IsValid ? Controller.CurrentFrame.Texture : null;
    }

    private void DrawFitted(BaseTexture texture, Matrix4x4 matrix)
    {
        Vector2 box = size;
        Vector2 frame = new(texture.Width, texture.Height);
        Color4 tint = UITint.Current * color;

        switch (fit)
        {
            case VideoFit.Contain:
            {
                float scale = MathF.Min(box.X / frame.X, box.Y / frame.Y);
                Vector2 drawn = frame * scale;
                Vector2 offset = (box - drawn) / 2;
                texture.tDraw2DMatrix(Matrix4x4.CreateTranslation(offset.X, offset.Y, 0) * matrix, drawn,
                    new RectangleF(0, 0, frame.X, frame.Y), tint);
                break;
            }

            case VideoFit.Cover:
            {
                float scale = MathF.Max(box.X / frame.X, box.Y / frame.Y);
                Vector2 shown = box / scale;
                Vector2 crop = (frame - shown) / 2;
                texture.tDraw2DMatrix(matrix, box, new RectangleF(crop.X, crop.Y, shown.X, shown.Y), tint);
                break;
            }

            default:
                texture.tDraw2DMatrix(matrix, box, new RectangleF(0, 0, frame.X, frame.Y), tint);
                break;
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Video"))
        {
            DTXMania.UI.Inspector.Inspector.Inspect("Source", ref source);
            DTXMania.UI.Inspector.Inspector.Inspect("Fit", ref fit);

            if (source == VideoSource.Linked)
            {
                ImGui.InputText("Link", ref link, 256);
                return;
            }

            DTXMania.UI.Inspector.ResourceEditor.Draw("File", ResourceType.Video, video, chosen =>
            {
                video = chosen;
                _lastVideoLoadAttempt = null;
                LoadDeclaredVideo();
            });

            if (ImGui.Button("Reload Video"))
            {
                _lastVideoLoadAttempt = null;
                LoadDeclaredVideo();
            }
        }

        if (source == VideoSource.Embedded)
        {
            Controller.DrawInspector(GetHashCode().ToString());
        }
    }

    public override void Dispose()
    {
        Controller.Dispose();
        base.Dispose();
    }
}
