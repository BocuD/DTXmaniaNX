using System.Drawing;
using DTXMania.Core;
using SharpDX.Direct3D9;
using FDK;

namespace DTXMania;

internal class CActOptionPanel : CActivity
{
    // CActivity 実装

    public override void OnDeactivate()
    {
        if (!bNotActivated)
        {
            CDTXMania.tReleaseTexture(ref txOptionPanel);
            base.OnDeactivate();
        }
    }
    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {
            txOptionPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\Screen option panels.png"), false);
            base.OnManagedCreateResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (!bNotActivated)
        {
            Device device = CDTXMania.app.Device;
            CConfigIni configIni = CDTXMania.ConfigIni;
            if (txOptionPanel != null)
            {
                int drums = configIni.nScrollSpeed.Drums;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(device, 0x2e2, 14, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Guitar;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(device, 0x2e2, 0x20, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Bass;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(device, 0x2e2, 50, rc譜面スピード[drums]);
                txOptionPanel.tDraw2D(device, 0x312, 14, rcHS[(configIni.bHidden.Drums ? 1 : 0) + (configIni.bSudden.Drums ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 0x312, 0x20, rcHS[(configIni.bHidden.Guitar ? 1 : 0) + (configIni.bSudden.Guitar ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 0x312, 50, rcHS[(configIni.bHidden.Bass ? 1 : 0) + (configIni.bSudden.Bass ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 0x342, 14, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 0x342, 0x20, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 0x342, 50, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 0x372, 14, rcReverse[configIni.bReverse.Drums ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x372, 0x20, rcReverse[configIni.bReverse.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x372, 50, rcReverse[configIni.bReverse.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 930, 14, rcPosition[(int)configIni.JudgementStringPosition.Drums]);
                txOptionPanel.tDraw2D(device, 930, 0x20, rcPosition[(int)configIni.JudgementStringPosition.Guitar]);
                txOptionPanel.tDraw2D(device, 930, 50, rcPosition[(int)configIni.JudgementStringPosition.Bass]);
                txOptionPanel.tDraw2D(device, 0x3d2, 14, rcTight[configIni.bTight ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x3d2, 0x20, rcRandom[(int)configIni.eRandom.Guitar]);
                txOptionPanel.tDraw2D(device, 0x3d2, 50, rcRandom[(int)configIni.eRandom.Bass]);
                txOptionPanel.tDraw2D(device, 0x402, 14, rcComboPos[(int)configIni.ドラムコンボ文字の表示位置]);
                txOptionPanel.tDraw2D(device, 0x402, 0x20, rcLight[configIni.bLight.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x402, 50, rcLight[configIni.bLight.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x432, 0x20, rcLeft[configIni.bLeft.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 0x432, 50, rcLeft[configIni.bLeft.Bass ? 1 : 0]);
            }
        }
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    private readonly Rectangle[] rcComboPos = new Rectangle[] { new Rectangle(0x60, 0x6c, 0x30, 0x12), new Rectangle(0x60, 90, 0x30, 0x12), new Rectangle(0x60, 0x48, 0x30, 0x12), new Rectangle(0x30, 0x6c, 0x30, 0x12) };
    private readonly Rectangle[] rcDark = new Rectangle[] { new Rectangle(0x30, 0, 0x30, 0x12), new Rectangle(0x30, 0x12, 0x30, 0x12), new Rectangle(0x30, 0x54, 0x30, 0x12) };
    private readonly Rectangle[] rcHS = new Rectangle[] { new Rectangle(0, 0, 0x30, 0x12), new Rectangle(0, 0x12, 0x30, 0x12), new Rectangle(0, 0x24, 0x30, 0x12), new Rectangle(0, 0x36, 0x30, 0x12) };
    private readonly Rectangle[] rcLeft = new Rectangle[] { new Rectangle(0xc0, 0x6c, 0x30, 0x12), new Rectangle(240, 0x6c, 0x30, 0x12) };
    private readonly Rectangle[] rcLight = new Rectangle[] { new Rectangle(240, 0x48, 0x30, 0x12), new Rectangle(240, 90, 0x30, 0x12) };
    private readonly Rectangle[] rcPosition = new Rectangle[] { new Rectangle(0, 0x48, 0x30, 0x12), new Rectangle(0, 90, 0x30, 0x12), new Rectangle(0, 0x6c, 0x30, 0x12) };
    private readonly Rectangle[] rcRandom = new Rectangle[] { new Rectangle(0x90, 0x48, 0x30, 0x12), new Rectangle(0x90, 90, 0x30, 0x12), new Rectangle(0x90, 0x6c, 0x30, 0x12), new Rectangle(0x90, 0x7e, 0x30, 0xb6) };
    private readonly Rectangle[] rcReverse = new Rectangle[] { new Rectangle(0x30, 0x24, 0x30, 0x12), new Rectangle(0x30, 0x36, 0x30, 0x12) };
    private readonly Rectangle[] rcTight = new Rectangle[] { new Rectangle(0xc0, 0x48, 0x30, 0x12), new Rectangle(0xc0, 90, 0x30, 0x12) };
    private readonly Rectangle[] rc譜面スピード = new Rectangle[] { new Rectangle(0x60, 0, 0x30, 0x12), new Rectangle(0x60, 0x12, 0x30, 0x12), new Rectangle(0x60, 0x24, 0x30, 0x12), new Rectangle(0x60, 0x36, 0x30, 0x12), new Rectangle(0x90, 0, 0x30, 0x12), new Rectangle(0x90, 0x12, 0x30, 0x12), new Rectangle(0x90, 0x24, 0x30, 0x12), new Rectangle(0x90, 0x36, 0x30, 0x12), new Rectangle(0xc0, 0, 0x30, 0x12), new Rectangle(0xc0, 0x12, 0x30, 0x12), new Rectangle(0xc0, 0x24, 0x30, 0x12), new Rectangle(0xc0, 0x36, 0x30, 0x12), new Rectangle(240, 0, 0x30, 0x12), new Rectangle(240, 0x12, 0x30, 0x12), new Rectangle(240, 0x24, 0x30, 0x12), new Rectangle(240, 0x36, 0x30, 0x12) };
    private CTexture txOptionPanel;
    //-----------------
    #endregion
}