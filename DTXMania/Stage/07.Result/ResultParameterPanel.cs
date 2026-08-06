using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;

namespace DTXMania;

/// <summary>
/// The result score/judgement panel: one row per judgement plus the score, all stamped from the shared
/// ResultRow component, so editing that one component restyles every row.
/// </summary>
public class ResultParameterPanel : UIItemsGroup, IUIItemSource
{
    private const float RowSpacing = 24.0f;

    private readonly ResultRowData[] rows;

    public int ItemCount => rows.Length;
    public object GetItem(int index) => rows[index];

    public ResultParameterPanel(int instrument) : base("ResultParameterPanel")
    {
        scale.X = 0.96f;

        var stageResult = CDTXMania.StageManager.stageResult;
        var pd = stageResult.stPerformanceEntry[instrument];

        rows =
        [
            Judgement("Perfect", pd.nPerfectCount, stageResult.fPerfectPercentage[instrument]),
            Judgement("Great", pd.nGreatCount, stageResult.fGreatPercentage[instrument]),
            Judgement("Good", pd.nGoodCount, stageResult.fGoodPercentage[instrument]),
            Judgement("Ok", pd.nPoorCount, stageResult.fPoorPercentage[instrument]),
            Judgement("Miss", pd.nMissCount, stageResult.fMissPercentage[instrument]),
            Judgement("Max Combo", pd.nMaxCombo, 100.0 * pd.nMaxCombo / pd.nTotalChipsCount),
            new ResultRowData { Label = "Score", Value = pd.nScore, Padding = 7 }
        ];

        itemComponent = "Components/ResultRow.json";
        itemOffset = new Vector3(0, RowSpacing, 0);
        itemDefault = BuildResultRowDefault;

        SetSource(this);
    }

    private static ResultRowData Judgement(string label, int count, double percentage) => new()
    {
        Label = label,
        Value = count,
        Padding = 4,
        Percent = (long)Math.Round(percentage),
        ShowPercent = true
    };

    //the code default for one row, seeded into Components/ResultRow.json
    private static UIGroup BuildResultRowDefault()
    {
        UIGroup root = new("ResultRow");

        UIText label = root.AddChild(new UIText("Label"));
        label.name = "Label";
        label.bindings.Add(new UIBinding("text", "Item.Label"));
        label.outlineWidth = 0;
        label.fontSize = 20;
        label.font = SkinResource.System("Futura PT Medium.otf");

        UIPaddedNumber count = root.AddChild(new UIPaddedNumber());
        count.name = "Count";
        count.fontSize = 23;
        count.position = new Vector3(107, -8, 0);
        count.bindings.Add(new UIBinding("value", "Item.Value"));
        count.bindings.Add(new UIBinding("padding", "Item.Padding"));

        UIPaddedNumber percent = root.AddChild(new UIPaddedNumber());
        percent.name = "Percent";
        percent.padding = 3;
        percent.fontSize = 21;
        percent.position = new Vector3(187, -8, 0);
        percent.bindings.Add(new UIBinding("isVisible", "Item.ShowPercent"));
        percent.bindings.Add(new UIBinding("value", "Item.Percent"));

        UIText percentSign = root.AddChild(new UIText("%"));
        percentSign.name = "PercentSign";
        percentSign.fillColor = Color4.White;
        percentSign.outlineWidth = 0;
        percentSign.position = new Vector3(224, 0, 0);
        percentSign.font = SkinResource.System("texgyreadventor-regular.otf");
        percentSign.fontSize = 15;
        percentSign.style = UiTextStyle.Bold;
        percentSign.anchor = new Vector2(0, 0);
        percentSign.bindings.Add(new UIBinding("isVisible", "Item.ShowPercent"));

        return root;
    }
}
