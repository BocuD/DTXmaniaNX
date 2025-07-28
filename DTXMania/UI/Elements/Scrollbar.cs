using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
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
        
        var bar = scrollbar.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_scrollbar.png"))));
        scrollbar.scrollBarImage = bar;
        
        bar.clipRect = new RectangleF(0, 0, 12, 492);
        bar.size = new Vector2(12, 492);
        
        var handle = scrollbar.AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\5_scrollbar.png"))));
        scrollbar.scrollBarHandleImage = handle;
        
        handle.clipRect = new RectangleF(0, 492, 12, 12);
        handle.size = new Vector2(12, 12);

        return scrollbar;
    }
    
    public DrawableReference<UIImage> scrollBarImage;
    public DrawableReference<UIImage> scrollBarHandleImage;

    public float progress = 0;
    
    public Scrollbar() : base("Scrollbar")
    {
        
    }

    public override void Draw(Matrix parentMatrix)
    {
        //update position of handle
        UIImage bar = scrollBarImage;
        UIImage handle = scrollBarHandleImage;
        
        var handleHeight = handle.clipRect.Height;
        var handleY = bar.clipRect.Height - handleHeight - (bar.clipRect.Height - handleHeight) * (1 - progress);
        handle.position.Y = handleY;
        
        base.Draw(parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Scrollbar"))
        {
            ImGui.SliderFloat("Progress", ref progress, 0f, 1f);
            
            Inspector.Inspector.Inspect("Bar Image", ref scrollBarImage);
            Inspector.Inspector.Inspect("Handle Image", ref scrollBarHandleImage);
        }
    }
}