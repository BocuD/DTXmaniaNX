using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;

namespace DTXMania;

/// <summary>
/// The result-screen progress bars: the panel frame plus the current-play and previous-best bars, generated
/// from the same progress-string data the performance stage produced. The bar textures are runtime, rebuilt
/// each time the stage loads.
/// </summary>
public class ResultProgressBar : UIGroup
{
    public ResultProgressBar(int instrument)
    {
        name = "ResultProgressBar";

        var stageResult = CDTXMania.StageManager.stageResult;

        var panel = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\8_progress_bar_panel.png"))));
        panel.name = "Panel";
        panel.renderOrder = 0;

        //the best-record bar is thin and sits behind the wider current-play bar
        BaseTexture bestBar = null!;
        CActPerfProgressBar.txGenerateProgressBarHelper(ref bestBar, stageResult.strBestProgressBarRecord[instrument],
            4, 425, CActPerfProgressBar.nSectionIntervalCount);

        BaseTexture currentBar = null!;
        CActPerfProgressBar.txGenerateProgressBarHelper(ref currentBar, stageResult.strCurrProgressBarRecord[instrument],
            12, 425, CActPerfProgressBar.nSectionIntervalCount);

        if (currentBar != null)
        {
            var current = AddChild(new UIImage(currentBar));
            current.name = "CurrentBar";
            current.position = new Vector3(1, 1, 0);
            current.renderOrder = 1;
        }

        if (bestBar != null)
        {
            var best = AddChild(new UIImage(bestBar));
            best.name = "BestBar";
            best.position = new Vector3(15, 1, 0);
            best.renderOrder = 2;
        }
    }
}
