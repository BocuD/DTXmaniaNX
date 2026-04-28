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

    public override unsafe int OnUpdateAndDraw()
    {
        if (bActivated)
        {
            if (bJustStartedUpdate)
            {
                //base.n進行用タイマ = CDTXMania.Timer.nCurrentTime;                    
                bJustStartedUpdate = false;
            }

            ctBonusScoreAnimationCounter[(int)EInstrumentPart.GUITAR].tUpdate();
            ctBonusScoreAnimationCounter[(int)EInstrumentPart.BASS].tUpdate();

            if (CDTXMania.ConfigIni.bShowScore)
            {
                drawBonusScoreAnimation(EInstrumentPart.GUITAR, new Point(333, 45));
                drawBonusScoreAnimation(EInstrumentPart.BASS, new Point(885, 45));                    
            }


        }
        return 0;
    }

    public void startBonus(EInstrumentPart eInstrument)
    {
        ctBonusScoreAnimationCounter[(int)eInstrument].tStart(0, 500, 1, CDTXMania.Timer);
    }

    private void drawBonusScoreAnimation(EInstrumentPart eInstrumentPart, Point pt)
    {
        if (!ctBonusScoreAnimationCounter[(int)eInstrumentPart].bReachedEndValue 
            && txBonus100 != null)
        {
            int nCounterValue = ctBonusScoreAnimationCounter[(int)eInstrumentPart].nCurrentValue;                
            txBonus100.tDraw2D(pt.X, pt.Y - nCounterValue / 25);
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