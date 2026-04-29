using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfGuitarGauge : CActPerfCommonGauge
{

    // コンストラクタ

    public CActPerfGuitarGauge()
    {
        bActivated = false;
    }


    // CActivity 実装

    public override void OnActivate()
    {
        n本体X.Guitar = 80;
        n本体X.Bass = 912 + 290 + 38;
        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        ct本体移動 = null;
        ct本体振動 = null;
        base.OnDeactivate();
    }
    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            txフレーム.Guitar = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Gauge_Guitar.png"));
            txフレーム.Bass = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Gauge_Bass.png"));
            txフルゲージ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_gauge_bar.jpg"));
            txゲージ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_gauge_bar.png"));

            //this.txマスクF = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            //this.txマスクD = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            txハイスピ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Panel_icons.jpg"));

            base.OnManagedCreateResources();
        }
    }

    public override int OnUpdateAndDraw()
    {
        //int num9;
        if (bJustStartedUpdate)
        {
            ct本体移動 = new CCounter(0, 0x1a, 20, CDTXMania.Timer);
            ct本体振動 = new CCounter(0, 360, 4, CDTXMania.Timer);
            bJustStartedUpdate = false;
        }
        ct本体移動.tUpdateLoop();
        ct本体振動.tUpdateLoop();
            
        #region [ ギターのゲージ ]
        if (txフレーム.Guitar != null && CDTXMania.DTX.bHasChips.Guitar)
        {
            txフレーム.Guitar.tDraw2D(n本体X.Guitar, 0, new RectangleF(0, 0, txフレーム.Guitar.Width, 68));
            //txハイスピ.vcScaleRatio = new Vector3(0.76190476190476190476190476190476f, 0.66666666666666666666666666666667f, 1.0f);
            //todo: what the hell is vcscaleratio
            int speedTexturePosY = CDTXMania.ConfigIni.nScrollSpeed.Guitar * 48 > 20 * 48 ? 20 * 48 : CDTXMania.ConfigIni.nScrollSpeed.Guitar * 48;
            txハイスピ.tDraw2D(- 36 + n本体X.Guitar + txフレーム.Guitar.Width, 30, new RectangleF(0, speedTexturePosY, 42, 48));
            if (db現在のゲージ値.Guitar == 1.0 && txフルゲージ != null)
            {
                txフルゲージ.tDraw2D(6 + n本体X.Guitar, 31, new RectangleF(0, 0, txフレーム.Guitar.Width - 48, 30));
            }
            else if (db現在のゲージ値.Guitar >= 0.0)
            {
                //todo: what the hell is vcscaleratio
                //txゲージ.vcScaleRatio.X = (float)db現在のゲージ値.Guitar;
                txゲージ.tDraw2D(6 + n本体X.Guitar, 31, new RectangleF(0, 0, txフレーム.Guitar.Width - 48, 30));
            }
            txフレーム.Guitar.tDraw2D(n本体X.Guitar, 0, new RectangleF(0, 68, txフレーム.Guitar.Width, 68));
            /*
            if (base.IsDanger(EInstrumentPart.GUITAR) && base.db現在のゲージ値.Guitar >= 0.0 && this.txマスクD != null)
            {
                this.txマスクD.tDraw2D(CDTXMania.app.Device, base.n本体X.Guitar, 0);
            }
            if (base.db現在のゲージ値.Guitar == 1.0 && this.txマスクF != null)
            {
                this.txマスクF.tDraw2D(CDTXMania.app.Device, base.n本体X.Guitar, 0);
            }
             */
        }
        #endregion

        #region [ ベースのゲージ ]
        if (txフレーム.Bass != null && CDTXMania.DTX.bHasChips.Bass)
        {
            txフレーム.Bass.tDraw2D(n本体X.Bass - txフレーム.Bass.Width, 0, new RectangleF(0, 0, txフレーム.Bass.Width, 68));
            //todo: what the fuck is vcscaleratio
            //txハイスピ.vcScaleRatio = new Vector3(0.76190476190476190476190476190476f, 0.66666666666666666666666666666667f, 1.0f);
            int speedTexturePosY = CDTXMania.ConfigIni.nScrollSpeed.Bass * 48 > 20 * 48 ? 20 * 48 : CDTXMania.ConfigIni.nScrollSpeed.Bass * 48;
            txハイスピ.tDraw2D(4 + n本体X.Bass - txフレーム.Bass.Width, 30, new RectangleF(0, speedTexturePosY, 42, 48));
            if (db現在のゲージ値.Bass == 1.0 && txフルゲージ != null)
            {
                txフルゲージ.tDraw2D(42 + n本体X.Bass - txフレーム.Bass.Width, 31, new RectangleF(0, 0, txフレーム.Bass.Width - 48, 30));
            }
            else if (db現在のゲージ値.Bass >= 0.0)
            {
                //todo: what the fuck is vsscaleratio.x
                //txゲージ.vcScaleRatio.X = (float)db現在のゲージ値.Bass;
                txゲージ.tDraw2D(42 + n本体X.Bass - txフレーム.Bass.Width, 31, new RectangleF(0, 0, txフレーム.Bass.Width - 48, 30));
            }
            txフレーム.Bass.tDraw2D(n本体X.Bass - txフレーム.Bass.Width, 0, new RectangleF(0, 68, txフレーム.Bass.Width, 68));
            /*
            if (base.IsDanger(EInstrumentPart.BASS) && base.db現在のゲージ値.Bass >= 0.0 && this.txマスクD != null)
            {
                this.txマスクD.tDraw2D(CDTXMania.app.Device, base.n本体X.Bass, 0);
            }
            if (base.db現在のゲージ値.Bass == 1.0 && this.txマスクF != null)
            {
                this.txマスクF.tDraw2D(CDTXMania.app.Device, base.n本体X.Bass, 0);
            }
             */
        }
        #endregion

        return 0;
    }

    // Other

    #region [ private ]
    //-----------------
    //-----------------
    #endregion
}