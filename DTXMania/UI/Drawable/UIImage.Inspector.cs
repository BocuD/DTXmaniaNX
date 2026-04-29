using DTXMania.Core;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using NativeFileDialog.Extended;

namespace DTXMania.UI.Drawable;

public partial class UIImage
{
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Image"))
        {
            return;
        }

        Inspector.Inspector.Inspect("Image Source", ref imageSource);
        if (imageSource == ImageSource.Resource)
        {
            ImGui.LabelText("Resource", resource);
        }
        Inspector.Inspector.Inspect("Clip Rect", ref clipRect);
        Inspector.Inspector.Inspect("Render Mode", ref renderMode);
        Inspector.Inspector.Inspect("Color", ref color);

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
                    string resourcePath = currentSkin.AddResource(ResourceType.Image, path);
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