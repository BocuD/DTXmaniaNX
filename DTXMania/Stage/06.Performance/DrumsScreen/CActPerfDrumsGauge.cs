using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania;

internal class CActPerfDrumsGauge : CActPerfCommonGauge
{

    // コンストラクタ

    public CActPerfDrumsGauge()
    {
        bActivated = false;
    }


    // CActivity 実装

    public override void OnActivate()
    {
        n本体X.Drums = 294;
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
            txフレーム.Drums = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Gauge.png"));
            txゲージ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_gauge_bar.png"));
            txフルゲージ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_gauge_bar.jpg"));

            //this.txマスクF = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            //this.txマスクD = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            txハイスピ = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_Panel_icons.jpg"));

            base.OnManagedCreateResources();
        }
    }

    public override int OnUpdateAndDraw()
    {

        if (bActivated)
        {

            if (txフレーム.Drums != null)
            {
                txフレーム.Drums.tDraw2D(n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626), new RectangleF(0, 0, txフレーム.Drums.Width, 47)); 
                int speedTexturePosY = CDTXMania.ConfigIni.nScrollSpeed.Drums * 48 > 20 * 48 ? 20 * 48 : CDTXMania.ConfigIni.nScrollSpeed.Drums * 48;
                txハイスピ.tDraw2D(-37 + n本体X.Drums + txフレーム.Drums.Width, (CDTXMania.ConfigIni.bReverse.Drums ? 35 : 634), new RectangleF(0, speedTexturePosY, 42, 48), Color4.White, new Vector2(32, 32));
                if (db現在のゲージ値.Drums == 1.0 && txフルゲージ != null)
                {
                    txフルゲージ.tDraw2D(20 + n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 37 : 635), new RectangleF(0, 0, txフレーム.Drums.Width - 63, 31));
                }
                else
                {
                    float xSize = (float)db現在のゲージ値.Drums * (txフレーム.Drums.Width - 63);
                    txゲージ.tDraw2D(20 + n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 37 : 635), new RectangleF(0, 0, txフレーム.Drums.Width - 63, 31), Color4.White, new Vector2(xSize, 31));
                }
                txフレーム.Drums.tDraw2D(n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626), new RectangleF(0, 47, txフレーム.Drums.Width, 47));
            }
            /*
            if (base.IsDanger(EInstrumentPart.DRUMS) && base.db現在のゲージ値.Drums >= 0.0)
            {
                this.txマスクD.tDraw2D(CDTXMania.app.Device, base.n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626));
            }
            if (base.db現在のゲージ値.Drums == 1.0)
            {
                this.txマスクF.tDraw2D(CDTXMania.app.Device, base.n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626));
            }
             */
        }
        return 0;
    }

    // Other

    #region [ private ]
    //-----------------
    //-----------------
    #endregion
}