using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

public enum ERenderMode
{
    Stretched,
    Sliced
}

public enum ImageSource
{
    //the built-in base skin, resolved against <exe>/System
    System,
    //a file the active custom skin owns, copied into its Resources folder
    Resource,
    //a live texture from a data context or the stage's dynamicImageSources
    Dynamic,
    //no file at all: a rectangle of this element's colour, for a dim behind a menu or a panel
    Solid
}


public partial class UIImage : UITexture
{
    [Themable] public RectangleF clipRect;
    [Themable] public RectangleF sliceRect;
    [Themable] public ERenderMode renderMode = ERenderMode.Stretched;
    [Themable] public ImageSource imageSource = ImageSource.System;
    [Themable] public string resource = string.Empty;

    //code-built images never get OnDeserialize, so Draw loads them lazily; tracking the attempted
    //resource stops a missing file from being retried every frame
    [JsonIgnore] private string? _lastFileLoadAttempt;

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
        if (imageSource == ImageSource.Dynamic)
        {
            UpdateDynamicTexture();
        }
        else if (imageSource == ImageSource.System && !texture.IsValid() && resource != _lastFileLoadAttempt)
        {
            _lastFileLoadAttempt = resource;
            LoadResource(updateRects: false);
        }
        else if (imageSource == ImageSource.Solid && !texture.IsValid())
        {
            //white, so the element's own colour is what shows; the size is the layout's to give
            SetTexture(BaseTexture.CreateSolidColor(Color4.White), updateRects: true, updateSize: false);
        }

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

    //pulls the current texture from the bound source, swapping only when it changes
    private void UpdateDynamicTexture()
    {
        if (!TryResolveContextTexture(resource, out BaseTexture current) || ReferenceEquals(current, texture))
        {
            return;
        }

        base.SetTexture(current, updateSize: false); //keep the size the layout specified

        //dynamic textures vary in size at runtime, so the clip extent tracks the current texture. Its
        //origin is left alone: that is authored, or bound, and must survive a texture swap.
        if (current.IsValid())
        {
            clipRect = new RectangleF(clipRect.X, clipRect.Y, current.Width, current.Height);
            sliceRect = clipRect;
        }
    }

    //a Dynamic image borrows its texture rather than owning it, so detach before disposing; System and
    //Resource images load from disk here and are disposed normally
    public override void Dispose()
    {
        if (imageSource == ImageSource.Dynamic)
        {
            texture = BaseTexture.None;
        }

        base.Dispose();
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
        else if (imageSource == ImageSource.System && !string.IsNullOrWhiteSpace(resource))
        {
            //resource is a System-relative graphics path, e.g. "Graphics\2_background.png"
            string fullPath = SkinManager.SystemPath(resource);
            if (!File.Exists(fullPath))
            {
                Trace.TraceError($"Image file {fullPath} does not exist.");
                SetTexture(BaseTexture.None);
                return;
            }

            BaseTexture loaded = BaseTexture.LoadFromPath(fullPath);
            base.SetTexture(loaded, updateSize: false);

            if (loaded.IsValid())
            {
                //fill clip/slice/size from the texture only where the layout left them at their defaults,
                //so a compact layout still draws while explicit values keep overriding. A size axis is
                //"unset" at 0 (SetTexture(None) from the ctor) or 1 (the field default), which lets an
                //image stretch one axis while the other follows the texture
                if (clipRect.IsEmpty)
                {
                    clipRect = new RectangleF(0, 0, loaded.Width, loaded.Height);
                }

                if (sliceRect.IsEmpty)
                {
                    sliceRect = clipRect;
                }

                if (size.X <= 1f) size.X = loaded.Width;
                if (size.Y <= 1f) size.Y = loaded.Height;
            }
        }
    }
}