using DTXMania.Core;
using DTXMania.UI.Inspector;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Drawable;

public class UIImage : UITexture
{
    public RectangleF clipRect;
    public RectangleF sliceRect;
    
    public ERenderMode renderMode = ERenderMode.Stretched;
    
    public ImageSource imageSource = ImageSource.File;
    public string resource = "";

    [AddChildMenu]
    public static UIDrawable Create()
    {
        return new UIImage();
    }
    
    public UIImage() : base(BaseTexture.None)
    {
    }
    
    public UIImage(BaseTexture texture) : base(texture)
    {
        if (texture.isValid())
        {
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
        }
    }

    public override void Draw(Matrix parentMatrix)
    {
        if (!isVisible) return;
        if (!texture.isValid()) return;
        
        UpdateLocalTransformMatrix();
            
        Matrix combinedMatrix = localTransformMatrix * parentMatrix;

        switch (renderMode)
        {
            case ERenderMode.Stretched:
                texture.tDraw2DMatrix(combinedMatrix, size, clipRect);
                break;
                
            case ERenderMode.Sliced:
                texture.tDraw2DMatrixSliced(combinedMatrix, size, clipRect, sliceRect);
                break;
        }
    }

    public void SetTexture(BaseTexture newTexture, bool updateRects = true)
    {
        texture = newTexture;
            
        if (!texture.isValid()) return;

        if (updateRects)
        {
            size = new Vector2(texture.Width, texture.Height);
            clipRect = new RectangleF(0, 0, texture.Width, texture.Height);
            sliceRect = new RectangleF(0, 0, texture.Width, texture.Height);
        }
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

            SetTexture(new DTXTexture(fullPath), updateRects);
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Image"))
        {
            Inspector.Inspector.Inspect("Image Source", ref imageSource);
            if (imageSource == ImageSource.Resource)
            {
                ImGui.SameLine();
                ImGui.LabelText("Resource: ", resource);
            }
            
            if (ImGui.Button("Load New Texture"))
            {
                //open windows file selection dialog
                Dictionary<string, string> filterList = new()
                {
                    { "Images", "png" }
                };
                
                string path = NativeFileDialog.Extended.NFD.OpenDialog("", filterList);

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
                        SetTexture(new DTXTexture(CDTXMania.tGenerateTexture(path)));
                    }
                }
            }

            Inspector.Inspector.Inspect("Clip Rect", ref clipRect);
            
            Inspector.Inspector.Inspect("Render Mode", ref renderMode);
            if (renderMode == ERenderMode.Sliced)
            {
                Inspector.Inspector.Inspect("Slice Rect", ref sliceRect);
            }
            
            //display texture
            if (texture.isValid())
            {
                float windowWidth = ImGui.GetWindowWidth();
                float textureWidth = windowWidth - 64;

                if (textureWidth > texture.Width * 3)
                    textureWidth = texture.Width * 3;

                float textureHeight = texture.Height * (textureWidth / texture.Width);

                //reserve space for the image
                ImGui.Dummy(new System.Numerics.Vector2(textureWidth, textureHeight));
                
                float scaleRatio = textureWidth / texture.Width;

                //image rect
                var pMin = ImGui.GetItemRectMin();
                var pMax = ImGui.GetItemRectMax();
                
                var textureId = texture.GetImTextureID();

                if (textureId != null)
                {
                    ImGui.GetWindowDrawList().AddImage(textureId.Value, pMin, pMax);
                    
                    //basic bounds
                    ImGui.GetWindowDrawList().AddRect(pMin, pMax, 0xFF00FF00, 0, 0, 2);
                    
                    //clip rect
                    var scaledClipRect = new RectangleF(
                        clipRect.X * scaleRatio,
                        clipRect.Y * scaleRatio,
                        clipRect.Width * scaleRatio,
                        clipRect.Height * scaleRatio
                    );
                    var clipRectMin = new System.Numerics.Vector2(pMin.X + scaledClipRect.X, pMin.Y + scaledClipRect.Y);
                    var clipRectMax = new System.Numerics.Vector2(pMin.X + scaledClipRect.X + scaledClipRect.Width, pMin.Y + scaledClipRect.Y + scaledClipRect.Height);
                    
                    //slice rect
                    if (renderMode == ERenderMode.Sliced)
                    {
                        var scaledSliceRect = new RectangleF(
                            sliceRect.X * scaleRatio,
                            sliceRect.Y * scaleRatio,
                            sliceRect.Width * scaleRatio,
                            sliceRect.Height * scaleRatio
                        );
                        var sliceRectMin = new System.Numerics.Vector2(pMin.X + scaledClipRect.X + scaledSliceRect.X, pMin.Y + scaledClipRect.Y + scaledSliceRect.Y);
                        var sliceRectMax = new System.Numerics.Vector2(pMin.X + scaledClipRect.X + scaledSliceRect.X + scaledSliceRect.Width, pMin.Y + scaledClipRect.Y + scaledSliceRect.Y + scaledSliceRect.Height);
                        ImGui.GetWindowDrawList().AddRect(sliceRectMin, sliceRectMax, 0xFF0000FF, 0, 0, 2);
                        
                        //add slice rect lines
                        float x1 = pMin.X + scaledClipRect.X;
                        float x2 = pMin.X + scaledClipRect.X + scaledSliceRect.X;
                        float x3 = pMin.X + scaledClipRect.X + scaledSliceRect.X + scaledSliceRect.Width;
                        float x4 = pMin.X + scaledClipRect.X + scaledClipRect.Width;
                        float y1 = pMin.Y + scaledClipRect.Y;
                        float y2 = pMin.Y + scaledClipRect.Y + scaledSliceRect.Y;
                        float y3 = pMin.Y + scaledClipRect.Y + scaledSliceRect.Y + scaledSliceRect.Height;
                        float y4 = pMin.Y + scaledClipRect.Y + scaledClipRect.Height;
                        
                        //left right lines
                        ImGui.GetWindowDrawList().AddLine(new System.Numerics.Vector2(x1, y2), new System.Numerics.Vector2(x4, y2), 0xFF0000FF, 1);
                        ImGui.GetWindowDrawList().AddLine(new System.Numerics.Vector2(x1, y3), new System.Numerics.Vector2(x4, y3), 0xFF0000FF, 1);
                        
                        //top bottom lines
                        ImGui.GetWindowDrawList().AddLine(new System.Numerics.Vector2(x2, y1), new System.Numerics.Vector2(x2, y4), 0xFF0000FF, 1);
                        ImGui.GetWindowDrawList().AddLine(new System.Numerics.Vector2(x3, y1), new System.Numerics.Vector2(x3, y4), 0xFF0000FF, 1);
                    }
                    
                    ImGui.GetWindowDrawList().AddRect(clipRectMin, clipRectMax, 0xFFFF0000, 0, 0, 2);
                }
            }
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