using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActResultRank : CActivity
{
    // コンストラクタ

    public CActResultRank()
    {
        bNotActivated = true;
    }


    // メソッド

    public void tアニメを完了させる()
    {
        ctランク表示.nCurrentValue = ctランク表示.nEndValue;
    }


    // CActivity 実装

    public override void OnActivate()
    {
        #region [ 本体位置 ]

        int n中X = 480;
        int n中Y = 0;

        int n左X = 300;
        int n左Y = -15;

        int n右X = 720;
        int n右Y = -15;

        n本体X[0] = 0;
        n本体Y[0] = 0;

        n本体X[1] = 0;
        n本体Y[1] = 0;

        n本体X[2] = 0;
        n本体Y[2] = 0;

        if (CDTXMania.ConfigIni.bDrumsEnabled)
        {
            n本体X[0] = n中X;
            n本体Y[0] = n中Y;
        }
        else if (CDTXMania.ConfigIni.bGuitarEnabled)
        {
            if (!CDTXMania.DTX.bHasChips.Bass)
            {
                n本体X[1] = n中X;
                n本体Y[1] = n中Y;
            }
            else if (!CDTXMania.DTX.bHasChips.Guitar)
            {
                n本体X[2] = n中X;
                n本体Y[2] = n中Y;
            }
            else if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
            {
                n本体X[1] = n右X;
                n本体Y[1] = n右Y;
                n本体X[2] = n左X;
                n本体Y[2] = n左Y;
            }
            else
            {
                n本体X[1] = n左X;
                n本体Y[1] = n左Y;
                n本体X[2] = n右X;
                n本体Y[2] = n右Y;
            }
        }
        #endregion

        b全オート.Drums = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay;
        b全オート.Guitar = CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay;
        b全オート.Bass = CDTXMania.ConfigIni.bAllBassAreAutoPlay;

        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        if (ctランク表示 != null)
        {
            ctランク表示 = null;
        }
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {

            txStageCleared = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenResult StageCleared.png"));
            txFullCombo = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenResult fullcombo.png"));
            txExcellent = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenResult Excellent.png"));

            for (int j = 0; j < 3; j++)
            {
                switch (CDTXMania.stageResult.nRankValue[j])
                {
                    case 0:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankSS.png"));
                        break;

                    case 1:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankS.png"));
                        break;

                    case 2:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankA.png"));
                        break;

                    case 3:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankB.png"));
                        break;

                    case 4:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankC.png"));
                        break;

                    case 5:
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankD.png"));
                        break;

                    case 6:
                    case 99:	// #23534 2010.10.28 yyagi: 演奏チップが0個のときは、rankEと見なす
                        txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankE.png"));
                        if (b全オート[j])
                            txランク文字[j] = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\8_rankSS.png"));
                        break;

                    default:
                        txランク文字[j] = null;
                        break;
                }
            }
            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            CDTXMania.tReleaseTexture(ref txStageCleared);
            CDTXMania.tReleaseTexture(ref txFullCombo);
            CDTXMania.tReleaseTexture(ref txExcellent);
            CDTXMania.tDisposeSafely(ref txランク文字);
            base.OnManagedReleaseResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (bNotActivated)
        {
            return 0;
        }
        if (bJustStartedUpdate)
        {
            ctランク表示 = new CCounter(0, 500, 1, CDTXMania.Timer);
            bJustStartedUpdate = false;
        }
        ctランク表示.tUpdate();

        for (int j = 0; j < 3; j++)
        {
            if (n本体X[j] != 0)
            {
                #region [ ランク文字 ]
                if (txランク文字[j] != null)
                {
                    double num2 = ((double)ctランク表示.nCurrentValue - 200.0) / 300.0;

                    if (ctランク表示.nCurrentValue >= 200.0)
                        txランク文字[j].tDraw2D(CDTXMania.app.Device, n本体X[j], n本体Y[j] + ((int)((double)txランク文字[j].szImageSize.Height * (1.0 - num2))), new Rectangle(0, 0, txランク文字[j].szImageSize.Width, (int)((double)txランク文字[j].szImageSize.Height * num2)));
                }
                #endregion

                #region [ フルコンボ ]
                int num14 = -165 + n本体X[j];
                int num15 = 100 + n本体Y[j];

                if (CDTXMania.stageResult.stPerformanceEntry[j].nPerfectCount == CDTXMania.stageResult.stPerformanceEntry[j].nTotalChipsCount)
                {
                    if (txExcellent != null)
                        txExcellent.tDraw2D(CDTXMania.app.Device, num14, num15);
                }
                else if (CDTXMania.stageResult.stPerformanceEntry[j].bIsFullCombo)
                {
                    if (txFullCombo != null)
                        txFullCombo.tDraw2D(CDTXMania.app.Device, num14, num15);
                }
                else
                {
                    if (txStageCleared != null)
                        txStageCleared.tDraw2D(CDTXMania.app.Device, num14, num15);
                }
                #endregion
            }
        }

        if (!ctランク表示.bReachedEndValue)
        {
            return 0;
        }
        return 1;
    }


    // Other

    #region [ private ]
    //-----------------
    private CCounter ctランク表示;
    private STDGBVALUE<int> n本体X;
    private STDGBVALUE<int> n本体Y;
    private STDGBVALUE<bool> b全オート;
    private STDGBVALUE<CTexture> txランク文字;
    private CTexture txStageCleared;
    private CTexture txFullCombo;
    private CTexture txExcellent;
    //-----------------
    #endregion
}