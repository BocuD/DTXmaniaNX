using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Drawable;

public class UIImage : UITexture
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
        if (texture.isValid())
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            sliceRect = clipRect;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        if (!isVisible || !texture.isValid())
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

        if (updateRects && texture.isValid())
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
            string? fullPath = CDTXMania.SkinManager.currentSkin?.GetResource(resource);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                SetTexture(BaseTexture.None);
                return;
            }

            SetTexture(BaseTexture.LoadFromPath(fullPath), updateRects);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Image"))
        {
            return;
        }

        Inspector.Inspector.Inspect("Image Source", ref imageSource);
        Inspector.Inspector.Inspect("Clip Rect", ref clipRect);
        Inspector.Inspector.Inspect("Render Mode", ref renderMode);
        Inspector.Inspector.Inspect("Color", ref color);

        if (imageSource == ImageSource.Resource)
        {
            ImGui.SameLine();
            ImGui.LabelText("Resource: ", resource);
        }

        if (ImGui.Button("Load New Texture"))
        {
            Dictionary<string, string> filterList = new()
            {
                { "Images", "png,jpg,jpeg,bmp,tga,gif" }
            };

            string path = NFD.OpenDialog("", filterList);

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var currentSkin = CDTXMania.SkinManager.currentSkin;
                    
                if (currentSkin != null)
                {
                    string resourcePath = currentSkin.AddResource(path);
                    imageSource = ImageSource.Resource;
                    resource = resourcePath;
                    LoadResource(true);
                }
                else
                {
                    SetTexture(BaseTexture.LoadFromPath(path));
                }
            }
        }

        if (renderMode == ERenderMode.Sliced)
        {
            Inspector.Inspector.Inspect("Slice Rect", ref sliceRect);
        }
    }
}

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
