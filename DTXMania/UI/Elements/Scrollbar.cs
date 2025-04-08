using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXUIRenderer;
using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Elements;

public class Scrollbar : UIGroup
{
    [AddChildMenu]
    public static Scrollbar Create()
    {
        var scrollbar = new Scrollbar();
        scrollbar.name = "New Scrollbar";
        
        scrollbar.scrollBarImage = scrollbar.AddChild(new UIImage(new DTXTexture(CSkin.Path(@"Graphics\5_scrollbar.png"))));
        scrollbar.scrollBarImage.clipRect = new RectangleF(0, 0, 12, 492);
        scrollbar.scrollBarImage.size = new Vector2(12, 492);
        
        scrollbar.scrollBarHandleImage = scrollbar.AddChild(new UIImage(new DTXTexture(CSkin.Path(@"Graphics\5_scrollbar.png"))));
        scrollbar.scrollBarHandleImage.clipRect = new RectangleF(0, 492, 12, 12);
        scrollbar.scrollBarHandleImage.size = new Vector2(12, 12);

        return scrollbar;
    }
    
    public UIImage scrollBarImage;
    public UIImage scrollBarHandleImage;

    public float progress = 0;
    
    public Scrollbar() : base("Scrollbar")
    {
        
    }

    public override void Draw(Matrix parentMatrix)
    {
        //update position of handle
        var handleHeight = scrollBarHandleImage.clipRect.Height;
        var handleY = scrollBarImage.clipRect.Height - handleHeight - (scrollBarImage.clipRect.Height - handleHeight) * (1 - progress);
        scrollBarHandleImage.position.Y = handleY;
        
        base.Draw(parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Scrollbar"))
        {
            ImGui.SliderFloat("Progress", ref progress, 0f, 1f);
        }
    }
}