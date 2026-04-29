using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;

namespace DTXMania.UI.Drawable;

public enum ERenderMode
{
    Stretched,
    Sliced
}

public enum ImageSource
{
    File,
    Resource,
    Dynamic
}


public partial class UIImage : UITexture
{
    [Themable] public RectangleF clipRect;
    [Themable] public RectangleF sliceRect;
    [Themable] public ERenderMode renderMode = ERenderMode.Stretched;
    [Themable] public ImageSource imageSource = ImageSource.File;
    [Themable] public string resource = string.Empty;

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIImage();
    }

    public UIImage()
        : base(BaseTexture.None)
    {
    }

    public UIImage(BaseTexture texture)
        : base(texture)
    {
        if (texture.IsValid())
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            sliceRect = clipRect;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible || !texture.IsValid())
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (renderMode == ERenderMode.Sliced)
        {
            texture.tDraw2DMatrixSliced(combinedMatrix, size, clipRect, color, sliceRect);
            return;
        }

        texture.tDraw2DMatrix(combinedMatrix, size, clipRect, color);
    }

    public void SetTexture(BaseTexture newTexture, bool updateRects = true, bool updateSize = true)
    {
        base.SetTexture(newTexture, updateSize);

        if (updateRects && texture.IsValid())
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            sliceRect = clipRect;
        }
    }
    
    public override void OnDeserialize()
    {
        base.OnDeserialize();
        
        LoadResource(false);
    }
    
    public void LoadResource(bool updateRects)
    {
        if (imageSource == ImageSource.Resource)
        {
            Trace.TraceInformation("Updating resource for " + id);
            string? fullPath = CDTXMania.SkinManager.currentSkin?.GetResource(ResourceType.Image, resource);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Trace.TraceError($"Resource {resource} not found in current skin.");
                SetTexture(BaseTexture.None);
                return;
            }
            if (!File.Exists(fullPath))
            {
                Trace.TraceError($"Resource file {fullPath} does not exist.");
                SetTexture(BaseTexture.None);
                return;
            }

            SetTexture(BaseTexture.LoadFromPath(fullPath), updateRects);
        }
    }
}