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

    public override void Dispose()
    {
        
    }
}