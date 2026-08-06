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

/// <summary>What an image draws. Only <see cref="File"/> is a location, so only it consults
/// <see cref="UIImage.image"/>; the other two are answered by the element itself.</summary>
public enum ImageSource
{
    //a file, wherever image says it lives
    File,
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
    [Themable] public ImageSource imageSource = ImageSource.File;

    [Themable] public SkinResource image;

    //the data key a Dynamic image reads its texture from
    [Themable] public string dynamicSource = string.Empty;

    //code-built images never get OnDeserialize, so Draw loads them lazily; tracking what was attempted
    //stops a missing file from being retried every frame
    [JsonIgnore] private SkinResource _lastFileLoadAttempt;

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
        else if (imageSource == ImageSource.File && !texture.IsValid() && image != _lastFileLoadAttempt)
        {
            _lastFileLoadAttempt = image;
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
        if (!TryResolveContextTexture(dynamicSource, out BaseTexture current) || ReferenceEquals(current, texture))
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

    //a Dynamic image borrows its texture rather than owning it, so detach before disposing; a File image
    //loaded it from disk and is disposed normally
    public override void Dispose()
    {
        if (imageSource == ImageSource.Dynamic)
        {
            texture = BaseTexture.None;
        }

        base.Dispose();
    }

    /// <summary>Loads what <see cref="image"/> points at. <paramref name="updateRects"/> makes the texture
    /// dictate the clip, slice and size outright, which is what picking a new file in the inspector wants;
    /// without it they are only filled in where the layout left them unset.</summary>
    public void LoadResource(bool updateRects)
    {
        if (imageSource != ImageSource.File || image.IsEmpty)
        {
            return;
        }

        string fullPath = image.Resolve(ResourceType.Image);

        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            Trace.TraceError($"Image {image} could not be found.");
            SetTexture(BaseTexture.None);
            return;
        }

        BaseTexture loaded = BaseTexture.LoadFromPath(fullPath);
        base.SetTexture(loaded, updateSize: false);

        if (!loaded.IsValid())
        {
            return;
        }

        //a compact layout writes none of these, so they follow the texture; anything the layout did state
        //keeps overriding it. A size axis is "unset" at 0 (SetTexture(None) from the ctor) or 1 (the field
        //default), which lets an image stretch one axis while the other follows the texture
        if (updateRects || clipRect.IsEmpty)
        {
            clipRect = new RectangleF(0, 0, loaded.Width, loaded.Height);
        }

        if (updateRects || sliceRect.IsEmpty)
        {
            sliceRect = clipRect;
        }

        if (updateRects || size.X <= 1f) size.X = loaded.Width;
        if (updateRects || size.Y <= 1f) size.Y = loaded.Height;
    }
}