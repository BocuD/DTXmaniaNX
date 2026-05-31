using System.Drawing;
using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania.Core.Video;

public class UINewVideoRenderer : UIDrawable
{
    public VideoPlayerController Controller { get; } = new();

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

    public UINewVideoRenderer(VideoPlayerController? controller = null)
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

    public override void Draw(Matrix4x4 parentMatrix)
    {
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
            frame.Texture.tDraw2DMatrix(combinedMatrix, size, clipRect, Color4.White);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        
        // Hand off rendering to the encapsulated Controller securely locked to this drawable instance.
        Controller.DrawInspector(id);
    }

    public override void Dispose()
    {
        Controller.Dispose();
        base.Dispose();
    }
}
