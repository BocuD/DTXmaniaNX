using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.Core.Video;

//mirrors ImageSource: System resolves against <exe>/System, Resource against the skin's Videos folder
public enum VideoSource
{
    System,
    Resource
}

public class UINewVideoRenderer : UIDrawable
{
    public VideoPlayerController Controller { get; } = new();

    //tint and opacity applied to the drawn frame, so callers can fade a video the way they would an image
    [Themable] public Color4 color = Color4.White;

    //with a resource set the video loads itself; leave it empty and call LoadVideo for a per-song one
    [Themable] public VideoSource videoSource = VideoSource.System;
    [Themable] public string resource = string.Empty;

    [JsonIgnore] private string? _lastVideoLoadAttempt;

    [AddChildMenu("Video/New Video Renderer")]
    public static UINewVideoRenderer CreateAsync()
    {
        return new UINewVideoRenderer();
    }
    
    [AddChildMenu("Video/New Video Renderer (Software Decoder)")]
    public static UINewVideoRenderer CreateSoftware()
    {
        return new UINewVideoRenderer { Controller = { UseSoftwareDecoder = true } };
    }

    //an all-optional constructor does not count as parameterless, and deserialization needs one
    public UINewVideoRenderer() : this(null)
    {
    }

    public UINewVideoRenderer(VideoPlayerController? controller)
    {
        if (controller != null)
        {
            Controller = controller;

            if (Controller.CurrentFrame.IsValid)
            {
                size = new Vector2(Controller.CurrentFrame.Texture.Width, Controller.CurrentFrame.Texture.Height);
            }
            else
            {
                size = new Vector2(640, 480);
            }
        }
        else
        {
            size = new Vector2(640, 480);
        }
    }

    public bool LoadVideo(string path)
    {
        if (!CDTXMania.ConfigIni.bAVIEnabled) return false;
        
        if (Controller.TryLoadVideo(path) && Controller.CurrentFrame.IsValid)
        {
            size = new Vector2(Controller.CurrentFrame.Texture.Width, Controller.CurrentFrame.Texture.Height);
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
        if (string.IsNullOrWhiteSpace(resource))
        {
            return;
        }

        _lastVideoLoadAttempt = resource;

        string? full = ResolveVideoPath();
        if (!string.IsNullOrWhiteSpace(full))
        {
            LoadVideo(full);
        }
    }

    private string? ResolveVideoPath()
    {
        if (videoSource == VideoSource.Resource)
        {
            return CDTXMania.SkinManager.currentSkin?.GetResource(ResourceType.Video, resource);
        }

        return SkinManager.SystemPath(resource);
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        //once per distinct resource, so a missing file is not retried every frame
        if (!string.IsNullOrWhiteSpace(resource) && resource != _lastVideoLoadAttempt)
        {
            LoadDeclaredVideo();
        }

        if (!isVisible) return;

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        // Controller pumps the decoder or relies on paused constraints natively.
        Controller.Update();

        DisplayedFrame frame = Controller.CurrentFrame;
        
        if (frame.IsValid && frame.Texture != null && frame.Texture.IsValid())
        {
            // Dynamically lock proportions if changed 
            if ((int)size.X != frame.Texture.Width || (int)size.Y != frame.Texture.Height)
            {
                //size = new Vector2(frame.Texture.Width, frame.Texture.Height);
            }
            
            RectangleF clipRect = new(0, 0, frame.Texture.Width, frame.Texture.Height);
            frame.Texture.tDraw2DMatrix(combinedMatrix, size, clipRect, color);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        DTXMania.UI.Inspector.Inspector.Inspect("Video Source", ref videoSource);
        if (ImGui.InputText("Video Path", ref resource, 512))
        {
            _lastVideoLoadAttempt = null;
        }
        if (ImGui.Button("Reload Video"))
        {
            _lastVideoLoadAttempt = null;
            LoadDeclaredVideo();
        }

        // Hand off rendering to the encapsulated Controller securely locked to this drawable instance.
        Controller.DrawInspector(id);
    }

    public override void Dispose()
    {
        Controller.Dispose();
        base.Dispose();
    }
}
