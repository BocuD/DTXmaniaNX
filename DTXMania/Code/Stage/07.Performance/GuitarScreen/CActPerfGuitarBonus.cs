using System.Drawing;
using FDK;

namespace DTXMania;

internal class CActPerfGuitarBonus : CActivity
{
    // コンストラクタ

    public CActPerfGuitarBonus()
    {
        bNotActivated = true;
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
        if (!bNotActivated)
        {
            txBonus100 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Bonus_100.png"));
            base.OnManagedCreateResources();
        }
    }

    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            CDTXMania.tReleaseTexture(ref txBonus100);
            base.OnManagedReleaseResources();
        }
    }

    public override unsafe int OnUpdateAndDraw()
    {
        if (!bNotActivated)
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
            txBonus100.tDraw2D(CDTXMania.app.Device, pt.X, pt.Y - nCounterValue / 25);
        }
    }

    // Other

    #region [ private ]
    //-----------------
    private CTexture txBonus100;
    private STDGBVALUE<CCounter> ctBonusScoreAnimationCounter;
    //-----------------
    #endregion
}