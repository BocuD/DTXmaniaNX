using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

public class LegacyDrawable : UIDrawable
{
    private Action<Matrix4x4> drawAction;

    public LegacyDrawable(string name, Action<Matrix4x4> drawAction)
    {
        this.name = name;
        this.drawAction = drawAction;
        dontSerialize = true;
    }
    
    public override void Draw(Matrix4x4 parentMatrix)
    {
        UpdateLocalTransformMatrix();
        drawAction(localTransformMatrix * parentMatrix);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        
        if (ImGui.CollapsingHeader("Legacy Drawable"))
        {
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