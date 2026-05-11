using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActResultRank : CActivity
{
    private CStageResult stageResult;
    
    public CActResultRank(CStageResult cStageResult)
    {
        stageResult = cStageResult;
        bActivated = false;
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

        bAllAuto.Drums = CDTXMania.ConfigIni.bAllDrumsAreAutoPlay;
        bAllAuto.Guitar = CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay;
        bAllAuto.Bass = CDTXMania.ConfigIni.bAllBassAreAutoPlay;

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
        if (bActivated)
        {
            txStageCleared = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\ScreenResult StageCleared.png"));
            txFullCombo = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\ScreenResult fullcombo.png"));
            txExcellent = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\ScreenResult Excellent.png"));

            for (int j = 0; j < 3; j++)
            {
                switch (stageResult.nRankValue[j])
                {
                    case 0:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankSS.png"));
                        break;

                    case 1:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankS.png"));
                        break;

                    case 2:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankA.png"));
                        break;

                    case 3:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankB.png"));
                        break;

                    case 4:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankC.png"));
                        break;

                    case 5:
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankD.png"));
                        break;

                    case 6:
                    case 99:	// #23534 2010.10.28 yyagi: 演奏チップが0個のときは、rankEと見なす
                        txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankE.png"));
                        if (bAllAuto[j])
                            txRankIcon[j] = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_rankSS.png"));
                        break;

                    default:
                        txRankIcon[j] = null;
                        break;
                }
            }
            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (bActivated)
        {
            base.OnManagedReleaseResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (!bActivated)
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
                if (txRankIcon[j] != null)
                {
                    txRankIcon[j].tDraw2D(n本体X[j], n本体Y[j] + ((int)((double)txRankIcon[j].Height * (1.0 - 1))), new RectangleF(0, 0, txRankIcon[j].Width, (int)((double)txRankIcon[j].Height * 1)));
                }
                #endregion

                #region [ フルコンボ ]
                int num14 = -165 + n本体X[j];
                int num15 = 100 + n本体Y[j];

                if (stageResult.stPerformanceEntry[j].nPerfectCount == stageResult.stPerformanceEntry[j].nTotalChipsCount)
                {
                    if (txExcellent != null)
                        txExcellent.tDraw2D(num14, num15);
                }
                else if (stageResult.stPerformanceEntry[j].bIsFullCombo)
                {
                    if (txFullCombo != null)
                        txFullCombo.tDraw2D(num14, num15);
                }
                else
                {
                    if (txStageCleared != null)
                        txStageCleared.tDraw2D(num14, num15);
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
    private STDGBVALUE<bool> bAllAuto;
    private STDGBVALUE<BaseTexture> txRankIcon;
    private BaseTexture txStageCleared;
    private BaseTexture txFullCombo;
    private BaseTexture txExcellent;
    //-----------------
    #endregion
}