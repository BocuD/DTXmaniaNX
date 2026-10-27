using System.Numerics;
using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfGuitarBonus : CActivity
{
    // コンストラクタ

    public CActPerfGuitarBonus()
    {
        bActivated = false;
    }

    public override void OnActivate()
    {
        ctBonusScoreAnimationCounter[(int)EInstrumentPart.GUITAR] = new CCounter();
        ctBonusScoreAnimationCounter[(int)EInstrumentPart.BASS] = new CCounter();

        base.OnActivate();
    }

    public override void OnDeactivate()
    {

        base.OnDeactivate();
    }

    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            txBonus100 = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Bonus_100.png"));
            base.OnManagedCreateResources();
        }
    }

    public void Draw(Matrix4x4 origin, EInstrumentPart part)
    {
        if (!bActivated)
        {
            return;
        }

        bJustStartedUpdate = false;
        ctBonusScoreAnimationCounter[(int)part].tUpdate();

        if (CDTXMania.ConfigIni.bShowScore)
        {
            drawBonusScoreAnimation(origin, part, new Point(333, 45));
        }
    }

    public void startBonus(EInstrumentPart eInstrument)
    {
        ctBonusScoreAnimationCounter[(int)eInstrument].tStart(0, 500, 1, CDTXMania.Timer);
    }

    private void drawBonusScoreAnimation(Matrix4x4 origin, EInstrumentPart eInstrumentPart, Point pt)
    {
        if (!ctBonusScoreAnimationCounter[(int)eInstrumentPart].bReachedEndValue 
            && txBonus100 != null)
        {
            int nCounterValue = ctBonusScoreAnimationCounter[(int)eInstrumentPart].nCurrentValue;                
            txBonus100.tDraw2D(origin, pt.X, pt.Y - nCounterValue / 25);
        }
    }

    // Other

    #region [ private ]
    //-----------------
    private BaseTexture txBonus100;
    private STDGBVALUE<CCounter> ctBonusScoreAnimationCounter;
    //-----------------
    #endregion
}