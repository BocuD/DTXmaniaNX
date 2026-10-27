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

public enum ImageFit
{
    Stretch,
    Contain,
    Cover
}

/// <summary>What an image draws. Only <see cref="File"/> is a location, so only it consults
/// <see cref="UIImage.image"/>.</summary>
public enum ImageSource
{
    //a file, wherever image says it lives
    File,
    //a live texture from a data context or the stage's dynamicImageSources
    Dynamic
}


public partial class UIImage : UITexture
{
    [Themable] public RectangleF clipRect;
    [Themable] public RectangleF sliceRect;
    [Themable] public ERenderMode renderMode = ERenderMode.Stretched;

    [Themable] public ImageFit fit = ImageFit.Stretch;
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

        if (!isVisible || !texture.IsValid())
        {
            return;
        }

        UpdateLocalTransformMatrix();
        Matrix4x4 combinedMatrix = localTransformMatrix * parentMatrix;

        if (renderMode == ERenderMode.Sliced)
        {
            texture.tDraw2DMatrixSliced(combinedMatrix, size, clipRect, UITint.Current * color, sliceRect);
            return;
        }

        DrawFitted(texture, combinedMatrix, size, clipRect, fit, UITint.Current * color);
    }

    public static void DrawFitted(BaseTexture texture, Matrix4x4 matrix, Vector2 box, RectangleF source, ImageFit fit,
        Color4 color)
    {
        Vector2 region = new(source.Width, source.Height);

        switch (fit)
        {
            case ImageFit.Contain:
            {
                float scale = MathF.Min(box.X / region.X, box.Y / region.Y);
                Vector2 drawn = region * scale;
                Vector2 offset = (box - drawn) / 2;
                texture.tDraw2DMatrix(Matrix4x4.CreateTranslation(offset.X, offset.Y, 0) * matrix, drawn, source, color);
                break;
            }

            case ImageFit.Cover:
            {
                float scale = MathF.Max(box.X / region.X, box.Y / region.Y);
                Vector2 shown = box / scale;
                Vector2 crop = (region - shown) / 2;
                texture.tDraw2DMatrix(matrix, box, new RectangleF(source.X + crop.X, source.Y + crop.Y, shown.X, shown.Y),
                    color);
                break;
            }

            default:
                texture.tDraw2DMatrix(matrix, box, source, color);
                break;
        }
    }

    //rects the layout never stated follow the texture; ones it did survive a swap
    public override void SetTexture(BaseTexture newTexture)
    {
        base.SetTexture(newTexture);

        if (texture.IsValid() && clipRect.IsEmpty)
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            sliceRect = clipRect;
        }
    }

    /// <summary>Lets the texture dictate the clip, slice and size outright, unlike <see cref="SetTexture"/>.</summary>
    public void SetTextureAndFit(BaseTexture newTexture)
    {
        base.SetTexture(newTexture);

        if (!texture.IsValid())
        {
            return;
        }

        clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
        sliceRect = clipRect;
        size.X = texture.Width;
        size.Y = texture.Height;
    }
    
    public override void OnDeserialize()
    {
        base.OnDeserialize();

        LoadResource(false);
    }

    //pulls the current texture from the bound source, swapping only when it changes
    private void UpdateDynamicTexture()
    {
        if (!this.TryResolveContextTexture(dynamicSource, out BaseTexture current) || ReferenceEquals(current, texture))
        {
            return;
        }

        base.SetTexture(current);

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
        base.SetTexture(loaded);

        if (!loaded.IsValid())
        {
            return;
        }

        //a compact layout writes none of these, so they follow the texture; anything the layout did state
        //keeps overriding it
        if (updateRects || clipRect.IsEmpty)
        {
            clipRect = new RectangleF(0, 0, loaded.Width, loaded.Height);
        }

        if (updateRects || sliceRect.IsEmpty)
        {
            sliceRect = clipRect;
        }

        //picking a file in the inspector re-sizes outright
        if (updateRects)
        {
            size.X = loaded.Width;
            size.Y = loaded.Height;
        }
    }
}