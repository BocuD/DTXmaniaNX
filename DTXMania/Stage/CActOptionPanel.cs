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
        if (bActivated)
        {
            CDTXMania.tReleaseTexture(ref txOptionPanel);
            base.OnDeactivate();
        }
    }
    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            txOptionPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\Screen option panels.png"), false);
            base.OnManagedCreateResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (bActivated)
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
                txOptionPanel.tDraw2D(device, 738, 14, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Guitar;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(device, 738, 32, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Bass;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(device, 738, 50, rc譜面スピード[drums]);
                txOptionPanel.tDraw2D(device, 786, 14, rcHS[(configIni.bHidden.Drums ? 1 : 0) + (configIni.bSudden.Drums ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 786, 32, rcHS[(configIni.bHidden.Guitar ? 1 : 0) + (configIni.bSudden.Guitar ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 786, 50, rcHS[(configIni.bHidden.Bass ? 1 : 0) + (configIni.bSudden.Bass ? 2 : 0)]);
                txOptionPanel.tDraw2D(device, 834, 14, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 834, 32, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 834, 50, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(device, 882, 14, rcReverse[configIni.bReverse.Drums ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 882, 32, rcReverse[configIni.bReverse.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 882, 50, rcReverse[configIni.bReverse.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 930, 14, rcPosition[(int)configIni.JudgementStringPosition.Drums]);
                txOptionPanel.tDraw2D(device, 930, 32, rcPosition[(int)configIni.JudgementStringPosition.Guitar]);
                txOptionPanel.tDraw2D(device, 930, 50, rcPosition[(int)configIni.JudgementStringPosition.Bass]);
                txOptionPanel.tDraw2D(device, 978, 14, rcTight[configIni.bTight ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 978, 32, rcRandom[(int)configIni.eRandom.Guitar]);
                txOptionPanel.tDraw2D(device, 978, 50, rcRandom[(int)configIni.eRandom.Bass]);
                txOptionPanel.tDraw2D(device, 1026, 14, rcComboPos[(int)configIni.ドラムコンボ文字の表示位置]);
                txOptionPanel.tDraw2D(device, 1026, 32, rcLight[configIni.bLight.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 1026, 50, rcLight[configIni.bLight.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 1074, 32, rcLeft[configIni.bLeft.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(device, 1074, 50, rcLeft[configIni.bLeft.Bass ? 1 : 0]);
            }
        }
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    private readonly SharpDX.RectangleF[] rcComboPos = [new(96, 108, 48, 18), new(96, 90, 48, 18), new(96, 72, 48, 18), new(48, 108, 48, 18)];
    private readonly SharpDX.RectangleF[] rcDark = [new(48, 0, 48, 18), new(48, 18, 48, 18), new(48, 84, 48, 18)];
    private readonly SharpDX.RectangleF[] rcHS = [new(0, 0, 48, 18), new(0, 18, 48, 18), new(0, 36, 48, 18), new(0, 54, 48, 18)];
    private readonly SharpDX.RectangleF[] rcLeft = [new(192, 108, 48, 18), new(240, 108, 48, 18)];
    private readonly SharpDX.RectangleF[] rcLight = [new(240, 72, 48, 18), new(240, 90, 48, 18)];
    private readonly SharpDX.RectangleF[] rcPosition = [new(0, 72, 48, 18), new(0, 90, 48, 18), new(0, 108, 48, 18)];
    private readonly SharpDX.RectangleF[] rcRandom = [new(144, 72, 48, 18), new(144, 90, 48, 18), new(144, 108, 48, 18), new(144, 126, 48, 182)];
    private readonly SharpDX.RectangleF[] rcReverse = [new(48, 36, 48, 18), new(48, 54, 48, 18)];
    private readonly SharpDX.RectangleF[] rcTight = [new(192, 72, 48, 18), new(192, 90, 48, 18)];
    private readonly SharpDX.RectangleF[] rc譜面スピード = [new(96, 0, 48, 18), new(96, 18, 48, 18), new(96, 36, 48, 18), new(96, 54, 48, 18), new(144, 0, 48, 18), new(144, 18, 48, 18), new(144, 36, 48, 18), new(144, 54, 48, 18), new(192, 0, 48, 18), new(192, 18, 48, 18), new(192, 36, 48, 18), new(192, 54, 48, 18), new(240, 0, 48, 18), new(240, 18, 48, 18), new(240, 36, 48, 18), new(240, 54, 48, 18)];
    private CTexture txOptionPanel;
    //-----------------
    #endregion
}