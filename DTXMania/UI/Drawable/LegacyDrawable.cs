using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public class LegacyDrawable : UIDrawable
{
    private Action drawAction;

    public LegacyDrawable(Action drawAction)
    {
        this.drawAction = drawAction;
        dontSerialize = true;
    }
    
    public LegacyDrawable(string name, Action drawAction)
    {
        this.name = name;
        this.drawAction = drawAction;
        dontSerialize = true;
    }
    
    public override void Draw(Matrix4x4 parentMatrix)
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
        base.Dispose();
        
        drawAction = null;
    }
}