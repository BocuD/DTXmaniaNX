using System.Numerics;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania;

//draws one player's chips. The stage sets the fields below every frame
public abstract class ChipRenderer : UIDrawable
{
    private static readonly int probe = AllocationProbe.Register("chip drawing");

    //chips further away than this are off screen
    protected const float DrawRange = 600;

    public List<CChip> chips = [];
    //-1 draws no chips
    public int firstChip = -1;
    public int judgeLineY;
    public bool showsLoopBegin;
    public bool showsLoopEnd;
    public int loopBeginDistance;
    public int loopEndDistance;

    protected ChipRenderer()
    {
        name = "Chips";
        dontSerialize = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        UpdateLocalTransformMatrix();

        AllocationProbe.Begin(probe);
        DrawChips(localTransformMatrix * parentMatrix);
        AllocationProbe.End(probe);
    }

    protected abstract void DrawChips(Matrix4x4 origin);

    //mode: 1 hidden, 2 sudden, 3 both, 4 stealth. Both fades are 50 units long. 0 is not drawn
    protected static float NoteAlpha(float distance, int mode, int suddenFrom, int hiddenFrom, float fadeDivisor)
    {
        bool hidden = mode == 1 || mode == 3;
        bool sudden = mode == 2 || mode == 3;

        if (mode == 4 || (hidden && distance < hiddenFrom))
        {
            return 0;
        }

        if (hidden && distance < hiddenFrom + 50)
        {
            return (distance - hiddenFrom) / fadeDivisor;
        }

        if (sudden && distance >= suddenFrom + 50)
        {
            return 0;
        }

        if (sudden && distance >= suddenFrom)
        {
            return 1 - (distance - suddenFrom) / fadeDivisor;
        }

        return 1;
    }
}
