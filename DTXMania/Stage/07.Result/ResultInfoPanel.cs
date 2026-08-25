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
/// Level / rate / skill block on the result screen. Pure layout: icons, divider lines and int/fraction
/// numbers bound to the stage's <c>"Result"</c> context (see <see cref="ResultData"/>).
/// </summary>
public class ResultInfoPanel : UIGroup
{
    public ResultInfoPanel()
    {
        name = "ResultInfo";

        var whiteTex = BaseTexture.CreateSolidColor(Color4.White);

        CreateLevelGroup(whiteTex);
        CreateRateGroup(whiteTex);
        CreateSkillGroup(whiteTex);
    }

    private static UIText DynamicNumber(UIGroup parent, string name, string source, int size, string font,
        Vector3 position, Vector2 anchor)
    {
        var text = parent.AddChild(new UIText("", size));
        text.name = name;
        text.bindings.Add(new UIBinding("text", source));
        text.position = position;
        text.anchor = anchor;
        text.font = SkinResource.System(font);
        text.outlineWidth = 0;
        return text;
    }

    private void CreateLevelGroup(BaseTexture white)
    {
        var levelGroup = AddChild(new UIGroup("Level"));

        var levelIcon = levelGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_level.png"))));
        levelIcon.position = new Vector3(64, 21, 0);
        levelIcon.name = "LevelIcon";

        var levelLine = levelGroup.AddChild(new UIImage(white));
        levelLine.position = new Vector3(88, 94, 0);
        levelLine.size = new Vector2(340, 2);
        levelLine.name = "LevelLine";

        DynamicNumber(levelGroup, "LevelNum", "Result.LevelInt", 61, "texgyreadventor-regular.otf",
            new Vector3(281, 107, 0), new Vector2(1, 1));
        DynamicNumber(levelGroup, "LevelFraction", "Result.LevelFraction", 50, "texgyreadventor-regular.otf",
            new Vector3(278, 102, 0), new Vector2(0, 1));
    }

    private void CreateRateGroup(BaseTexture white)
    {
        var rateGroup = AddChild(new UIGroup("Rate"));

        var rateIcon = rateGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Result\icon_rate.png"))));
        rateIcon.position = new Vector3(32, 77, 0);
        rateIcon.name = "RateIcon";

        var rateLine = rateGroup.AddChild(new UIImage(white));
        rateLine.position = new Vector3(60, 168, 0);
        rateLine.size = new Vector2(344, 2);
        rateLine.name = "RateLine";

        DynamicNumber(rateGroup, "RateNum", "Result.RateInt", 60, "texgyreadventor-regular.otf",
            new Vector3(281, 180, 0), new Vector2(1, 1));
        DynamicNumber(rateGroup, "RateFraction", "Result.RateFraction", 50, "texgyreadventor-regular.otf",
            new Vector3(278, 176, 0), new Vector2(0, 1));
    }

    private void CreateSkillGroup(BaseTexture whiteTex)
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

        var skillLine = skillGroup.AddChild(new UIImage(whiteTex));
        skillLine.position = new Vector3(14, 296, 0);
        skillLine.size = new Vector2(340, 2);
        skillLine.name = "SkillLine";

        var skillInt = DynamicNumber(skillGroup, "SkillNum", "Result.SkillInt", 82, "texgyreadventor-italic.otf",
            new Vector3(315, 299, 0), new Vector2(1, 1));
        skillInt.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillInt.texturePadding.X = 50;

        var skillFraction = DynamicNumber(skillGroup, "SkillFractionNum", "Result.SkillFraction", 53, "texgyreadventor-italic.otf",
            new Vector3(266, 290, 0), new Vector2(0, 1));
        skillFraction.style = UiTextStyle.Italic | UiTextStyle.Bold;
        skillFraction.texturePadding.X = 50;

        CreateSkillBar(skillGroup);
    }

    private static void CreateSkillBar(UIGroup skillGroup)
    {
        var fill = skillGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_skillbar_fill.png"))));
        fill.name = "SkillBarFill";
        fill.position = new Vector3(93, 319, 0);
        fill.anchor = new Vector2(0.0f, 0.5f);
        fill.size = new Vector2(286, 8);
        fill.renderOrder = 1;
        fill.isVisible = false;
        fill.bindings.Add(new UIBinding("isVisible", "Result.ShowSkillBar"));
        fill.bindings.Add(new UIBinding("size.X", "Result.SkillBarWidth"));

        var frame = skillGroup.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\5_skillbar.png"))));
        frame.name = "SkillBar";
        frame.position = new Vector3(14, 318, 0);
        frame.anchor = new Vector2(0.0f, 0.5f);
        frame.renderOrder = 2;
        frame.isVisible = false;
        frame.bindings.Add(new UIBinding("isVisible", "Result.ShowSkillBar"));
    }
}
