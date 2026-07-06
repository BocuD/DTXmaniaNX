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
        
        var whiteTex = BaseTexture.CreateSolidColor(Color4.White);

        CreateLevelGroup(instrument, whiteTex);
        CreateRateGroup(instrument, whiteTex);
        CreateSkillGroup(instrument, whiteTex);
    }

    private void CreateLevelGroup(int instrument, BaseTexture white)
    {
        var levelGroup = AddChild(new UIGroup("Level"));
        
        var levelIcon = levelGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_level.png"))));
        levelIcon.position = new Vector3(64, 21, 0);
        levelIcon.name = "LevelIcon";
        
        var levelLine = levelGroup.AddChild(new UIImage(white));
        levelLine.position = new Vector3(88, 94, 0);
        levelLine.size = new Vector2(340, 2);
        levelLine.name = "LevelLine";
        
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
        
        var levelInt = levelGroup.AddChild(new UIText(DTXLevel.ToString(), 61));
        levelInt.position = new Vector3(281, 107, 0);
        levelInt.anchor = new Vector2(1, 1);
        levelInt.fontSource = FontSource.System;
        levelInt.font = "texgyreadventor-regular.otf";
        levelInt.outlineWidth = 0;
        levelInt.name = "LevelNum";
        
        if (DTXLevelDeci.ToString().Length == 1)
        {
            DTXLevelDeci *= 10;
        }
        var levelFractionText = levelGroup.AddChild(new UIText("." + DTXLevelDeci, 50));
        levelFractionText.position = new Vector3(278, 102, 0);
        levelFractionText.anchor = new Vector2(0, 1);
        levelFractionText.fontSource = FontSource.System;
        levelFractionText.font = "texgyreadventor-regular.otf";
        levelFractionText.outlineWidth = 0;
        levelFractionText.name = "LevelFraction";
    }
    
    private void CreateRateGroup(int instrument, BaseTexture white)
    {
        var rateGroup = AddChild(new UIGroup("Rate"));
        
        var rateIcon = rateGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_rate.png"))));
        rateIcon.position = new Vector3(32, 77, 0);
        rateIcon.name = "RateIcon";
        
        var rateLine  = rateGroup.AddChild(new UIImage(white));
        rateLine.position = new Vector3(60, 168, 0);
        rateLine.size = new Vector2(344, 2);
        rateLine.name = "RateLine";

        double rate = CDTXMania.StageManager.stageResult.stPerformanceEntry[instrument].dbPerformanceSkill;
        int rateIntPart = (int)rate;
        int rateFraction = (int)((rate - rateIntPart) * 100);
        var rateInt = rateGroup.AddChild(new UIText(rateIntPart.ToString(), 60));
        rateInt.position = new Vector3(281, 180, 0);
        rateInt.anchor = new Vector2(1, 1);
        rateInt.fontSource = FontSource.System;
        rateInt.font = "texgyreadventor-regular.otf";
        rateInt.outlineWidth = 0;
        rateInt.name = "RateNum";
        
        if (rateFraction.ToString().Length == 1)
        {
            rateFraction *= 10;
        }
        
        var rateFractionText = rateGroup.AddChild(new UIText("." + rateFraction + "%", 50));
        rateFractionText.position = new Vector3(278, 176, 0);
        rateFractionText.anchor = new Vector2(0, 1);
        rateFractionText.fontSource = FontSource.System;
        rateFractionText.font = "texgyreadventor-regular.otf";
        rateFractionText.outlineWidth = 0;
        rateFractionText.name = "RateFraction";
    }
    
    private void CreateSkillGroup(int instrument, BaseTexture whiteTex)
    {
        var skillGroup = AddChild(new UIGroup("Skill"));
        
        var skillIcon = skillGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_skill.png"))));
        skillIcon.position = new Vector3(7, 194, 0);
        skillIcon.scale = new Vector3(0.67f, 0.67f, 1.0f);
        skillIcon.name = "SkillIcon";
        
        var skillText = skillGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\label_skill.png"))));
        skillText.position = new Vector3(18, 264, 0);
        skillText.scale = new Vector3(0.67f, 0.67f, 1.0f);
        skillText.name = "SkillText";
        
        var skillLine  = skillGroup.AddChild(new UIImage(whiteTex));
        skillLine.position = new Vector3(14, 296, 0);
        skillLine.size = new Vector2(340, 2);
        skillLine.name = "SkillLine";
        
        double skill = CDTXMania.StageManager.stageResult.stPerformanceEntry[instrument].dbGameSkill;
        int skillIntPart = (int)skill;
        int skillFraction = (int)((skill - skillIntPart) * 100);
        var skillInt = skillGroup.AddChild(new UIText(skillIntPart.ToString(), 82));
        skillInt.position = new Vector3(315, 299, 0);
        skillInt.anchor = new Vector2(1, 1);
        skillInt.fontSource = FontSource.System;
        skillInt.font = "texgyreadventor-italic.otf";
        skillInt.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillInt.texturePadding.X = 50;
        skillInt.outlineWidth = 0;
        skillInt.name = "SkillNum";
        
        var skillFractionText = skillGroup.AddChild(new UIText("." + skillFraction.ToString("N0"), 53));
        skillFractionText.position = new Vector3(266, 290, 0);
        skillFractionText.anchor = new Vector2(0, 1);
        skillFractionText.fontSource = FontSource.System;
        skillFractionText.font = "texgyreadventor-italic.otf";
        skillFractionText.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillFractionText.texturePadding.X = 50;
        skillFractionText.outlineWidth = 0;
        skillFractionText.name = "SkillFractionNum";
    }
}