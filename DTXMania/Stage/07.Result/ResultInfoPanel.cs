using DTXMania.UI.Skin;
using System.Numerics;
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
    public ResultInfoPanel() : base("ResultInfo")
    {
        MakeComponent("ResultInfoPanel", ResultInfoPanelDefault);
    }

    //the code default, also the seed for Components/ResultInfoPanel.json
    private static UIGroup ResultInfoPanelDefault()
    {
        UIGroup root = new("ResultInfo");

        CreateLevelGroup(root);
        CreateRateGroup(root);
        CreateSkillGroup(root);

        return root;
    }

    private static UIImage Icon(UIGroup parent, string name, string file, Vector3 position)
    {
        UIImage icon = parent.AddChild(new UIImage
        {
            name = name,
            imageSource = ImageSource.File,
            image = SkinResource.System(file),
            position = position
        });

        return icon;
    }

    private static void Divider(UIGroup parent, string name, Vector3 position, float width)
    {
        parent.AddChild(new UIImage
        {
            name = name,
            imageSource = ImageSource.Solid,
            position = position,
            size = new Vector2(width, 2)
        });
    }

    private static UIText DynamicNumber(UIGroup parent, string name, string source, int size, string font,
        Vector3 position, Vector2 pivot)
    {
        var text = parent.AddChild(new UIText("", size));
        text.name = name;
        text.bindings.Add(new UIBinding("text", source));
        text.position = position;
        text.pivot = pivot;
        text.font = SkinResource.System(font);
        text.outlineWidth = 0;
        return text;
    }

    private static void CreateLevelGroup(UIGroup root)
    {
        var levelGroup = root.AddChild(new UIGroup("Level"));

        Icon(levelGroup, "LevelIcon", @"Graphics\Result\icon_level.png", new Vector3(64, 21, 0));
        Divider(levelGroup, "LevelLine", new Vector3(88, 94, 0), 340);

        DynamicNumber(levelGroup, "LevelNum", "Result.LevelInt", 61, "texgyreadventor-regular.otf",
            new Vector3(281, 107, 0), new Vector2(1, 1));
        DynamicNumber(levelGroup, "LevelFraction", "Result.LevelFraction", 50, "texgyreadventor-regular.otf",
            new Vector3(278, 102, 0), new Vector2(0, 1));
    }

    private static void CreateRateGroup(UIGroup root)
    {
        var rateGroup = root.AddChild(new UIGroup("Rate"));

        Icon(rateGroup, "RateIcon", @"Graphics\Result\icon_rate.png", new Vector3(32, 77, 0));
        Divider(rateGroup, "RateLine", new Vector3(60, 168, 0), 344);

        DynamicNumber(rateGroup, "RateNum", "Result.RateInt", 60, "texgyreadventor-regular.otf",
            new Vector3(281, 180, 0), new Vector2(1, 1));
        DynamicNumber(rateGroup, "RateFraction", "Result.RateFraction", 50, "texgyreadventor-regular.otf",
            new Vector3(278, 176, 0), new Vector2(0, 1));
    }

    private static void CreateSkillGroup(UIGroup root)
    {
        var skillGroup = root.AddChild(new UIGroup("Skill"));

        Icon(skillGroup, "SkillIcon", @"Graphics\Result\icon_skill.png", new Vector3(7, 194, 0))
            .scale = new Vector3(0.67f, 0.67f, 1.0f);
        Icon(skillGroup, "SkillText", @"Graphics\Result\label_skill.png", new Vector3(18, 264, 0))
            .scale = new Vector3(0.67f, 0.67f, 1.0f);

        Divider(skillGroup, "SkillLine", new Vector3(14, 296, 0), 340);

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
        skillGroup.AddChild(new UIImage
        {
            name = "SkillBarFill",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_skillbar_fill.png"),
            position = new Vector3(155, 285, 0),
            pivot = new Vector2(0.0f, 0.5f),
            size = new Vector2(286, 10),
            renderOrder = 1,
            isVisible = false,
            bindings =
            {
                new UIBinding("isVisible", "Result.ShowSkillBar"),
                new UIBinding("size.X", "Result.SkillBarWidth")
            }
        });

        skillGroup.AddChild(new UIImage
        {
            name = "SkillBar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\Result\bar.png"),
            position = new Vector3(148, 285, 0),
            pivot = new Vector2(0.0f, 0.5f),
            scale = new Vector3(0.66f, 0.66f, 1.0f),
            renderOrder = 2,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Result.ShowSkillBar") }
        });
    }
}
