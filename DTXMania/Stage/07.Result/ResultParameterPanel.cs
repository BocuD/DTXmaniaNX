using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;

namespace DTXMania;

public class ResultParameterPanel : UIGroup
{
    public ResultParameterPanel(int instrument)
    {
        name = "ResultParameterPanel";
        sortByRenderOrder = false;
        scale.X = 0.96f;
        
        var stageResult = CDTXMania.StageManager.stageResult;
        var performanceData = stageResult.stPerformanceEntry[instrument];
        
        AddRow("Perfect", performanceData.nPerfectCount.ToString("D4"), $"{(int)Math.Round(stageResult.fPerfectPercentage[instrument]),3:##0}%");
        AddRow("Great", performanceData.nGreatCount.ToString("D4"), $"{(int)Math.Round(stageResult.fGreatPercentage[instrument]),3:##0}%");
        AddRow("Good", performanceData.nGoodCount.ToString("D4"), $"{(int)Math.Round(stageResult.fGoodPercentage[instrument]),3:##0}%");
        AddRow("Ok", performanceData.nPoorCount.ToString("D4"), $"{(int)Math.Round(stageResult.fPoorPercentage[instrument]),3:##0}%");
        AddRow("Miss", performanceData.nMissCount.ToString("D4"), $"{(int)Math.Round(stageResult.fMissPercentage[instrument]),3:##0}%");
        AddRow("Max Combo", performanceData.nMaxCombo.ToString("D4"), $"{(int)Math.Round((100.0 * stageResult.stPerformanceEntry[instrument].nMaxCombo / stageResult.stPerformanceEntry[instrument].nTotalChipsCount)),3:##0}%");
        AddRow("Score", performanceData.nScore.ToString("D7"));
    }
    
    private float yPos;
    public void AddRow(string name, string label = "", string percentage = "")
    {
        var text = AddChild(new UIText(name));
        text.name = name + "Label";
        text.outlineWidth = 0;
        text.fontSize = 20;
        text.fontSource = FontSource.System;
        text.font = "Futura PT Medium.otf";
        text.position.Y = yPos;

        var numText = AddChild(new UIText(label));
        numText.name = name + "Value";
        numText.outlineWidth = 0;
        numText.fontSize = 24;
        numText.fontSource = FontSource.System;
        numText.font = "Futura PT Medium.otf";
        numText.position = new Vector3(163, yPos - 5, 0);
        numText.anchor = new Vector2(1, 0);
        //numText.fillColor = new Color4(0.31f, 0.31f, 0.31f);
        
        var percentageText = AddChild(new UIText(percentage));
        percentageText.name = name + "Percentage";
        percentageText.outlineWidth = 0;
        percentageText.fontSize = 22;
        percentageText.fontSource = FontSource.System;
        percentageText.font = "Futura PT Medium.otf";
        percentageText.position = new Vector3(245, yPos - 4, 0);
        percentageText.anchor = new Vector2(1, 0);

        yPos += 24.0f;
    }
}