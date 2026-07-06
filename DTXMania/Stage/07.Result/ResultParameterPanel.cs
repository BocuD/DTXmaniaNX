using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;

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
        
        AddRow("Perfect", performanceData.nPerfectCount, (int)Math.Round(stageResult.fPerfectPercentage[instrument]));
        AddRow("Great", performanceData.nGreatCount, (int)Math.Round(stageResult.fGreatPercentage[instrument]));
        AddRow("Good", performanceData.nGoodCount, (int)Math.Round(stageResult.fGoodPercentage[instrument]));
        AddRow("Ok", performanceData.nPoorCount, (int)Math.Round(stageResult.fPoorPercentage[instrument]));
        AddRow("Miss", performanceData.nMissCount, (int)Math.Round(stageResult.fMissPercentage[instrument]));
        AddRow("Max Combo", performanceData.nMaxCombo, (int)Math.Round((100.0 * stageResult.stPerformanceEntry[instrument].nMaxCombo / stageResult.stPerformanceEntry[instrument].nTotalChipsCount)));
        AddRow("Score", performanceData.nScore);
    }
    
    private float yPos;
    public void AddRow(string name, long num1, long num2 = -1)
    {
        var row = AddChild(new UIGroup(name));
        var text = row.AddChild(new UIText(name));
        text.name = name + "Label";
        text.outlineWidth = 0;
        text.fontSize = 20;
        text.fontSource = FontSource.System;
        text.font = "Futura PT Medium.otf";
        text.position.Y = yPos;
        
        if (num2 != -1)
        {
            AddZeroStyledNumberText(row, name, new Vector3(107, yPos - 8, 0), num1, 4, "texgyreadventor-regular.otf", 23);
            AddZeroStyledNumberText(row, name + "_percentage", new Vector3(187, yPos - 8, 0), num2, 3, "texgyreadventor-regular.otf", 21);
            var percentSign = row.AddChild(new UIText("%"));
            percentSign.fillColor = Color4.White;
            percentSign.outlineWidth = 0;
            percentSign.position = new Vector3(224, yPos, 0);
            percentSign.font = "texgyreadventor-regular.otf";
            percentSign.fontSize = 15;
            percentSign.style = UiTextStyle.Bold;
            percentSign.anchor = new Vector2(0, 0);
        }
        else
        {
            AddZeroStyledNumberText(row, name, new Vector3(107, yPos - 8, 0), num1, 7, "texgyreadventor-regular.otf", 23);
        }
        yPos += 24.0f;
    }

    public void AddZeroStyledNumberText(UIGroup row, string name, Vector3 textPosition, long number, int padding, 
        string font, int fontSize)
    {
        string num = number.ToString("D" + padding);

        int zeroCount = 0;
        foreach (char t in num)
        {
            if (t == '0') zeroCount++;
            else break;
        }
        if (zeroCount == num.Length) zeroCount = num.Length - 1;
        
        string paddingString = num.Substring(0, zeroCount);
        string numString = num.Substring(zeroCount);
        
        var padText = row.AddChild(new UIText(paddingString));
        padText.name = name + "Pad";
        padText.outlineWidth = 0;
        padText.fontSize = fontSize;
        padText.fontSource = FontSource.System;
        padText.font = "texgyreadventor-regular.otf";
        padText.style = UiTextStyle.Bold;
        padText.position = textPosition;
        padText.fillColor = new Color4(0.31f, 0.31f, 0.31f);
        padText.RenderTexture();
        
        var numText = row.AddChild(new UIText(numString));
        numText.name = name + "Num";
        numText.outlineWidth = 0;
        numText.fontSize = fontSize;
        numText.fontSource = FontSource.System;
        numText.font = font;
        numText.style = UiTextStyle.Bold;
        numText.position = new Vector3(textPosition.X + (padText.Texture.Width * (1 / CDTXMania.renderScale)), textPosition.Y, 0);
    }
}