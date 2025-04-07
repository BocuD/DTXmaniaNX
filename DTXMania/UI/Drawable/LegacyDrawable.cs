using Hexa.NET.ImGui;
using SharpDX;

namespace DTXMania.UI.Drawable;

public class LegacyDrawable : UIDrawable
{
    private Action drawAction;
    
    public LegacyDrawable(Action drawAction)
    {
        this.drawAction = drawAction;
    }
    
    public override void Draw(Matrix parentMatrix)
    {
        drawAction();
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        
        if (ImGui.CollapsingHeader("Legacy Drawable"))
        {
            ImGui.Text("This is a legacy drawable.\nEditing any property other than render order will have no effect.");
            
            //display info about the method that is called
            ImGui.Text("Draw method: " + drawAction.Method.Name);
        }
    }

    public override void Dispose()
    {
        drawAction = null;
    }
}