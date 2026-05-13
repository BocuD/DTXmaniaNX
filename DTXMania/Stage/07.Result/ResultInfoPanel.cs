using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Text;

namespace DTXMania;

public class ResultInfoPanel : UIGroup
{
    public ResultInfoPanel(int instrument)
    {
        name = "ResultInfo";

        var stageResult = CDTXMania.StageManager.stageResult;
        
        var white = BaseTexture.CreateSolidColor(Color4.White);
        
        var levelIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_level.png"))));
        levelIcon.position = new Vector3(64, 21, 0);
        var levelLine = AddChild(new UIImage(white));
        levelLine.position = new Vector3(88, 94, 0);
        levelLine.size = new Vector2(340, 2);

        int DTXLevel;
        int DTXLevelDeci;
        if (CDTXMania.DTX.LEVEL[instrument] > 99)
        {
            DTXLevel = CDTXMania.DTX.LEVEL[instrument] / 100;
            DTXLevelDeci = CDTXMania.DTX.LEVEL[instrument] - (DTXLevel * 100);
        }
        else
        {
            DTXLevel = CDTXMania.DTX.LEVEL[instrument] / 10;
            DTXLevelDeci = ((CDTXMania.DTX.LEVEL[instrument] - DTXLevel * 10) * 10) + CDTXMania.DTX.LEVELDEC[instrument];
        }
        
        var levelInt = AddChild(new UIText(DTXLevel.ToString(), 61));
        levelInt.position = new Vector3(281, 107, 0);
        levelInt.anchor = new Vector2(1, 1);
        levelInt.fontSource = FontSource.System;
        levelInt.font = "texgyreadventor-regular.otf";
        levelInt.outlineWidth = 0;
        
        var levelFractionText = AddChild(new UIText("." + DTXLevelDeci, 50));
        levelFractionText.position = new Vector3(278, 102, 0);
        levelFractionText.anchor = new Vector2(0, 1);
        levelFractionText.fontSource = FontSource.System;
        levelFractionText.font = "texgyreadventor-regular.otf";
        levelFractionText.outlineWidth = 0;
        
        var rateIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_rate.png"))));
        rateIcon.position = new Vector3(32, 77, 0);
        var rateLine  = AddChild(new UIImage(white));
        rateLine.position = new Vector3(60, 168, 0);
        rateLine.size = new Vector2(344, 2);

        double rate = stageResult.stPerformanceEntry[instrument].dbPerformanceSkill;
        int rateIntPart = (int)rate;
        int rateFraction = (int)((rate - rateIntPart) * 100);
        var rateInt = AddChild(new UIText(rateIntPart.ToString(), 60));
        rateInt.position = new Vector3(281, 180, 0);
        rateInt.anchor = new Vector2(1, 1);
        rateInt.fontSource = FontSource.System;
        rateInt.font = "texgyreadventor-regular.otf";
        rateInt.outlineWidth = 0;
        
        var rateFractionText = AddChild(new UIText("." + rateFraction + "%", 50));
        rateFractionText.position = new Vector3(278, 176, 0);
        rateFractionText.anchor = new Vector2(0, 1);
        rateFractionText.fontSource = FontSource.System;
        rateFractionText.font = "texgyreadventor-regular.otf";
        rateFractionText.outlineWidth = 0;
        
        var skillIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_skill.png"))));
        skillIcon.position = new Vector3(7, 194, 0);
        skillIcon.scale = new Vector3(0.67f, 0.67f, 1.0f);
        
        var skillText = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\label_skill.png"))));
        skillText.position = new Vector3(18, 264, 0);
        skillText.scale = new Vector3(0.67f, 0.67f, 1.0f);
        var skillLine  = AddChild(new UIImage(white));
        skillLine.position = new Vector3(14, 296, 0);
        skillLine.size = new Vector2(340, 2);
        
        double skill = stageResult.stPerformanceEntry[instrument].dbGameSkill;
        int skillIntPart = (int)skill;
        int skillFraction = (int)((skill - skillIntPart) * 100);
        var skillInt = AddChild(new UIText(skillIntPart.ToString(), 82));
        skillInt.position = new Vector3(315, 299, 0);
        skillInt.anchor = new Vector2(1, 1);
        skillInt.fontSource = FontSource.System;
        skillInt.font = "texgyreadventor-italic.otf";
        skillInt.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillInt.texturePadding.X = 50;
        skillInt.outlineWidth = 0;
        
        var skillFractionText = AddChild(new UIText("." + skillFraction.ToString("N0"), 53));
        skillFractionText.position = new Vector3(266, 290, 0);
        skillFractionText.anchor = new Vector2(0, 1);
        skillFractionText.fontSource = FontSource.System;
        skillFractionText.font = "texgyreadventor-italic.otf";
        skillFractionText.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillFractionText.texturePadding.X = 50;
        skillFractionText.outlineWidth = 0;
    }
}