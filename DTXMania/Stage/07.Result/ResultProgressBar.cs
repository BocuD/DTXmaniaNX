using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania;

public class ResultProgressBar : UIGroup
{
    [Themable] public int currentBarWidth = 12;
    [Themable] public int bestBarWidth = 4;
    [Themable] public int barHeight = 425;

    private readonly UIDataContext data = new();

    private BaseTexture? currentBar;
    private BaseTexture? bestBar;
    private bool drawn;

    public ResultProgressBar() : base("ResultProgressBar")
    {
        data.RegisterTexture("Bars.Current", CurrentBar);
        data.RegisterTexture("Bars.Best", BestBar);
        dataContext = data;

        MakeComponent("ResultProgressBar", ProgressBarDefault);
    }

    private static UIGroup ProgressBarDefault()
    {
        UIGroup root = new("ResultProgressBar");

        root.AddChild(new UIImage
        {
            name = "Panel",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\8_progress_bar_panel.png"),
            renderOrder = 0
        });

        root.AddChild(new UIImage
        {
            name = "CurrentBar",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Bars.Current",
            position = new Vector3(1, 1, 0),
            renderOrder = 1
        });

        root.AddChild(new UIImage
        {
            name = "BestBar",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Bars.Best",
            position = new Vector3(15, 1, 0),
            renderOrder = 2
        });

        return root;
    }

    private BaseTexture? CurrentBar()
    {
        EnsureBars();
        return currentBar;
    }

    private BaseTexture? BestBar()
    {
        EnsureBars();
        return bestBar;
    }

    public void Regenerate() => drawn = false;

    //each bar is a surface, so they are drawn once until something asks again
    private void EnsureBars()
    {
        if (drawn)
        {
            return;
        }

        drawn = true;

        CStageResult stageResult = CDTXMania.StageManager.stageResult;
        int instrument = CDTXMania.GetCurrentInstrument();

        CActPerfProgressBar.txGenerateProgressBarHelper(ref bestBar!,
            stageResult.strBestProgressBarRecord[instrument],
            bestBarWidth, barHeight, CActPerfProgressBar.nSectionIntervalCount);

        CActPerfProgressBar.txGenerateProgressBarHelper(ref currentBar!,
            stageResult.strCurrProgressBarRecord[instrument],
            currentBarWidth, barHeight, CActPerfProgressBar.nSectionIntervalCount);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.Button("Regenerate Bars"))
        {
            Regenerate();
        }
    }
}
