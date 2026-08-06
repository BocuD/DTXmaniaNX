using DTXMania.Core.Framework;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

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
        switch (imageSource)
        {
            case ImageSource.File:
                Inspector.ResourceEditor.Draw("Image", ResourceType.Image, image, chosen =>
                {
                    image = chosen;
                    _lastFileLoadAttempt = chosen;
                    LoadResource(updateRects: true);
                }, inPlace => SetTexture(BaseTexture.LoadFromPath(inPlace)));

                if (ImGui.Button("Reload Image"))
                {
                    LoadResource(updateRects: false);
                }

                break;

            case ImageSource.Dynamic:
                Inspector.Inspector.DrawBindingDropdown("Dynamic Source", ref dynamicSource, this, DataBindingKind.Texture);
                break;
        }

        Inspector.Inspector.Inspect("Clip Rect", ref clipRect);
        Inspector.Inspector.Inspect("Render Mode", ref renderMode);
        Inspector.Inspector.Inspect("Color", ref color);

        if (renderMode == ERenderMode.Sliced)
        {
            Inspector.Inspector.Inspect("Slice Rect", ref sliceRect);
        }
    }
}
