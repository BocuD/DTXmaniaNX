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

        double level = CDTXMania.chosenChartData.SongInformation.LevelDec[instrument];
        
        int levelIntPart = (int)level;
        int levelFraction = (int)((level - levelIntPart) * 100);
        var levelInt = AddChild(new UIText(((int)level) + ".", 68));
        levelInt.position = new Vector3(295, 108, 0);
        levelInt.anchor = new Vector2(1, 1);
        levelInt.fontSource = FontSource.System;
        levelInt.font = "Futura PT Light.otf";
        levelInt.outlineWidth = 0;
        
        var levelFractionText = AddChild(new UIText(levelFraction.ToString(), 56));
        levelFractionText.position = new Vector3(290, 105, 0);
        levelFractionText.anchor = new Vector2(0, 1);
        levelFractionText.fontSource = FontSource.System;
        levelFractionText.font = "Futura PT Light.otf";
        levelFractionText.outlineWidth = 0;
        
        var rateIcon = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_rate.png"))));
        rateIcon.position = new Vector3(32, 77, 0);
        var rateLine  = AddChild(new UIImage(white));
        rateLine.position = new Vector3(60, 168, 0);
        rateLine.size = new Vector2(344, 2);

        double rate = stageResult.stPerformanceEntry[instrument].dbPerformanceSkill;
        int rateIntPart = (int)rate;
        int rateFraction = (int)((rate - rateIntPart) * 100);
        var rateInt = AddChild(new UIText(((int)rate) + ".", 68));
        rateInt.position = new Vector3(295, 182, 0);
        rateInt.anchor = new Vector2(1, 1);
        rateInt.fontSource = FontSource.System;
        rateInt.font = "Futura PT Light.otf";
        rateInt.outlineWidth = 0;
        
        var rateFractionText = AddChild(new UIText(rateFraction + "%", 56));
        rateFractionText.position = new Vector3(290, 179, 0);
        rateFractionText.anchor = new Vector2(0, 1);
        rateFractionText.fontSource = FontSource.System;
        rateFractionText.font = "Futura PT Light.otf";
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
        var skillInt = AddChild(new UIText(((int)skill) + ".", 94));
        skillInt.position = new Vector3(318, 302, 0);
        skillInt.anchor = new Vector2(1, 1);
        skillInt.fontSource = FontSource.System;
        skillInt.font = "Futura PT Medium.otf";
        skillInt.style = UiTextStyle.Italic;
        skillInt.texturePadding.X = 10;
        skillInt.outlineWidth = 0;
        
        var skillFractionText = AddChild(new UIText(skillFraction.ToString("N0"), 60));
        skillFractionText.position = new Vector3(305, 293, 0);
        skillFractionText.anchor = new Vector2(0, 1);
        skillFractionText.fontSource = FontSource.System;
        skillFractionText.font = "Futura PT Medium.otf";
        skillFractionText.style = UiTextStyle.Italic;
        skillFractionText.texturePadding.X = 10;
        skillFractionText.outlineWidth = 0;
    }
}