using System.Drawing;
using DTXMania.Core;
using FDK;
using BaseTexture = DTXMania.UI.Drawable.BaseTexture;

namespace DTXMania;

internal class CActOptionPanel : CActivity
{
    public override void OnManagedCreateResources()
    {
        if (bActivated)
        {
            txOptionPanel = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Screen option panels.png"));
            base.OnManagedCreateResources();
        }
    }
    public override int OnUpdateAndDraw()
    {
        if (bActivated)
        {
            CConfigIni configIni = CDTXMania.ConfigIni;
            if (txOptionPanel != null)
            {
                int drums = configIni.nScrollSpeed.Drums;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(738, 14, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Guitar;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(738, 32, rc譜面スピード[drums]);
                drums = configIni.nScrollSpeed.Bass;
                if (drums > 15)
                {
                    drums = 15;
                }
                txOptionPanel.tDraw2D(738, 50, rc譜面スピード[drums]);
                txOptionPanel.tDraw2D(786, 14, rcHS[(configIni.bHidden.Drums ? 1 : 0) + (configIni.bSudden.Drums ? 2 : 0)]);
                txOptionPanel.tDraw2D(786, 32, rcHS[(configIni.bHidden.Guitar ? 1 : 0) + (configIni.bSudden.Guitar ? 2 : 0)]);
                txOptionPanel.tDraw2D(786, 50, rcHS[(configIni.bHidden.Bass ? 1 : 0) + (configIni.bSudden.Bass ? 2 : 0)]);
                txOptionPanel.tDraw2D(834, 14, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(834, 32, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(834, 50, rcDark[(int)configIni.eDark]);
                txOptionPanel.tDraw2D(882, 14, rcReverse[configIni.bReverse.Drums ? 1 : 0]);
                txOptionPanel.tDraw2D(882, 32, rcReverse[configIni.bReverse.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(882, 50, rcReverse[configIni.bReverse.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(930, 14, rcPosition[(int)configIni.JudgementStringPosition.Drums]);
                txOptionPanel.tDraw2D(930, 32, rcPosition[(int)configIni.JudgementStringPosition.Guitar]);
                txOptionPanel.tDraw2D(930, 50, rcPosition[(int)configIni.JudgementStringPosition.Bass]);
                txOptionPanel.tDraw2D(978, 14, rcTight[configIni.bTight ? 1 : 0]);
                txOptionPanel.tDraw2D(978, 32, rcRandom[(int)configIni.eRandom.Guitar]);
                txOptionPanel.tDraw2D(978, 50, rcRandom[(int)configIni.eRandom.Bass]);
                txOptionPanel.tDraw2D(1026, 14, rcComboPos[(int)configIni.ドラムコンボ文字の表示位置]);
                txOptionPanel.tDraw2D(1026, 32, rcLight[configIni.bLight.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(1026, 50, rcLight[configIni.bLight.Bass ? 1 : 0]);
                txOptionPanel.tDraw2D(1074, 32, rcLeft[configIni.bLeft.Guitar ? 1 : 0]);
                txOptionPanel.tDraw2D(1074, 50, rcLeft[configIni.bLeft.Bass ? 1 : 0]);
            }
        }
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    private readonly RectangleF[] rcComboPos = [new(96, 108, 48, 18), new(96, 90, 48, 18), new(96, 72, 48, 18), new(48, 108, 48, 18)];
    private readonly RectangleF[] rcDark = [new(48, 0, 48, 18), new(48, 18, 48, 18), new(48, 84, 48, 18)];
    private readonly RectangleF[] rcHS = [new(0, 0, 48, 18), new(0, 18, 48, 18), new(0, 36, 48, 18), new(0, 54, 48, 18)];
    private readonly RectangleF[] rcLeft = [new(192, 108, 48, 18), new(240, 108, 48, 18)];
    private readonly RectangleF[] rcLight = [new(240, 72, 48, 18), new(240, 90, 48, 18)];
    private readonly RectangleF[] rcPosition = [new(0, 72, 48, 18), new(0, 90, 48, 18), new(0, 108, 48, 18)];
    private readonly RectangleF[] rcRandom = [new(144, 72, 48, 18), new(144, 90, 48, 18), new(144, 108, 48, 18), new(144, 126, 48, 182)];
    private readonly RectangleF[] rcReverse = [new(48, 36, 48, 18), new(48, 54, 48, 18)];
    private readonly RectangleF[] rcTight = [new(192, 72, 48, 18), new(192, 90, 48, 18)];
    private readonly RectangleF[] rc譜面スピード = [new(96, 0, 48, 18), new(96, 18, 48, 18), new(96, 36, 48, 18), new(96, 54, 48, 18), new(144, 0, 48, 18), new(144, 18, 48, 18), new(144, 36, 48, 18), new(144, 54, 48, 18), new(192, 0, 48, 18), new(192, 18, 48, 18), new(192, 36, 48, 18), new(192, 54, 48, 18), new(240, 0, 48, 18), new(240, 18, 48, 18), new(240, 36, 48, 18), new(240, 54, 48, 18)];
    private BaseTexture txOptionPanel;
    //-----------------
    #endregion
}