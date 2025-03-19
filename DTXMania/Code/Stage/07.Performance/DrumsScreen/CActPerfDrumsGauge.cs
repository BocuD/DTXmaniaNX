using SharpDX;
using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActPerfDrumsGauge : CActPerfCommonGauge
{

    // コンストラクタ

    public CActPerfDrumsGauge()
    {
        bNotActivated = true;
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
        if (!bNotActivated && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
        {
            txフレーム.Drums = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Gauge.png"));
            txゲージ = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_gauge_bar.png"));
            txフルゲージ = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_gauge_bar.jpg"));

            //this.txマスクF = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            //this.txマスクD = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Dummy.png"));
            txハイスピ = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_Panel_icons.jpg"));

            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
        {
            CDTXMania.tReleaseTexture(ref txフレーム.Drums);
            CDTXMania.tReleaseTexture(ref txゲージ);
            CDTXMania.tReleaseTexture(ref txフルゲージ);

            //CDTXMania.tReleaseTexture(ref this.txマスクF);
            //CDTXMania.tReleaseTexture(ref this.txマスクD);
            CDTXMania.tReleaseTexture(ref txハイスピ);

            base.OnManagedReleaseResources();
        }
    }
    public override int OnUpdateAndDraw()
    {

        if (!bNotActivated)
        {

            if (txフレーム.Drums != null)
            {
                txフレーム.Drums.tDraw2D(CDTXMania.app.Device, n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626), new Rectangle(0, 0, txフレーム.Drums.szImageSize.Width, 47));
                txハイスピ.vcScaleRatio = new Vector3(0.76190476190476190476190476190476f, 0.66666666666666666666666666666667f, 1.0f);
                int speedTexturePosY = CDTXMania.ConfigIni.nScrollSpeed.Drums * 48 > 20 * 48 ? 20 * 48 : CDTXMania.ConfigIni.nScrollSpeed.Drums * 48;
                txハイスピ.tDraw2D(CDTXMania.app.Device, -37 + n本体X.Drums + txフレーム.Drums.szImageSize.Width, (CDTXMania.ConfigIni.bReverse.Drums ? 35 : 634), new Rectangle(0, speedTexturePosY, 42, 48));
                if (db現在のゲージ値.Drums == 1.0 && txフルゲージ != null)
                {
                    txフルゲージ.tDraw2D(CDTXMania.app.Device, 20 + n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 37 : 635), new Rectangle(0, 0, txフレーム.Drums.szImageSize.Width - 63, 31));
                }
                else
                {
                    txゲージ.vcScaleRatio.X = (float)db現在のゲージ値.Drums;
                    txゲージ.tDraw2D(CDTXMania.app.Device, 20 + n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 37 : 635), new Rectangle(0, 0, txフレーム.Drums.szImageSize.Width - 63, 31));
                }
                txフレーム.Drums.tDraw2D(CDTXMania.app.Device, n本体X.Drums, (CDTXMania.ConfigIni.bReverse.Drums ? 28 : 626), new Rectangle(0, 47, txフレーム.Drums.szImageSize.Width, 47));
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