using System.Drawing;
using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Inspector;
using DTXMania.UI.Text;

namespace DTXMania;

public class UIPlayerNameplate : UIGroup
{
    [AddChildMenu]
    public new static UIPlayerNameplate Create()
    {
        return new UIPlayerNameplate();
    }
    
    //serialized so a nameplate loaded from a layout still knows whose it is
    [Themable] public int instrument;
    [Themable] public bool displaySkill;

    //found once the component has loaded; the name's colour is not something a binding can carry
    private UIText? playerNameText;

    private readonly UIDataContext data = new();

    public UIPlayerNameplate() : this(0)
    {
    }

    public UIPlayerNameplate(int instrument, bool displaySkill = false) : base("Nameplate")
    {
        this.instrument = instrument;
        this.displaySkill = displaySkill;

        data.RegisterObject("Player", () => new NameplateData(this));
        dataContext = data;

        size = new Vector2(266, 96);

        MakeComponent("Nameplate", NameplateDefault);
    }

    protected override void OnContentLoaded()
    {
        playerNameText = GetChild<UIText>("PlayerNameText");
        UpdateNameplate();
    }

    //the code default, also the seed for Components/Nameplate.json
    private static UIGroup NameplateDefault()
    {
        UIGroup root = new("Nameplate");

        //only the result screen shows a skill total, so the plate behind it comes and goes with it
        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System("Graphics/nameplate_bg.png"),
            renderOrder = -1,
            bindings = { new UIBinding("isVisible", "Player.ShowSkill") }
        });

        UIText title = root.AddChild(new UIText());
        title.name = "TitleText";
        title.position = new Vector3(11, 10, 0);
        title.font = SkinResource.System(UIFonts.FallbackFont);
        title.fontSize = 12;
        title.fillColor = Color.White;
        title.bindings.Add(new UIBinding("text", "Player.Title"));

        UIText playerName = root.AddChild(new UIText());
        playerName.name = "PlayerNameText";
        playerName.position = new Vector3(13, 28, 0);
        playerName.font = SkinResource.System(UIFonts.FallbackFont);
        playerName.fontSize = 20;
        playerName.outlineWidth = 0;
        playerName.bindings.Add(new UIBinding("text", "Player.Name"));

        UIText skill = root.AddChild(new UIText("", 24));
        skill.name = "SkillText";
        skill.position = new Vector3(159, 53, 0);
        skill.bindings.Add(new UIBinding("text", "Player.Skill"));
        skill.bindings.Add(new UIBinding("isVisible", "Player.ShowSkill"));

        return root;
    }

    /// <summary>Applies the colour the player picked. Bindings carry text, not colours, so this is the one
    /// part of the plate that is still set by hand.</summary>
    public void UpdateNameplate()
    {
        if (playerNameText == null)
        {
            return;
        }

        int colorIndex = CDTXMania.ConfigIni.nNameColor[instrument];

        Color clNameColor = Color.White;
        Color clNameColorLower = Color.White;

        switch (colorIndex)
        {
            case 0:
                clNameColor = Color.White;
                break;
            case 1:
                clNameColor = Color.LightYellow;
                break;
            case 2:
                clNameColor = Color.Yellow;
                break;
            case 3:
                clNameColor = Color.Green;
                break;
            case 4:
                clNameColor = Color.Blue;
                break;
            case 5:
                clNameColor = Color.Purple;
                break;
            case 6:
                clNameColor = Color.Red;
                break;
            case 7:
                clNameColor = Color.Brown;
                break;
            case 8:
                clNameColor = Color.Silver;
                break;
            case 9:
                clNameColor = Color.Gold;
                break;

            case 10:
                clNameColor = Color.White;
                break;
            case 11:
                clNameColor = Color.LightYellow;
                clNameColorLower = Color.White;
                break;
            case 12:
                clNameColor = Color.Yellow;
                clNameColorLower = Color.White;
                break;
            case 13:
                clNameColor = Color.FromArgb(0, 255, 33);
                clNameColorLower = Color.White;
                break;
            case 14:
                clNameColor = Color.FromArgb(0, 38, 255);
                clNameColorLower = Color.White;
                break;
            case 15:
                clNameColor = Color.FromArgb(72, 0, 255);
                clNameColorLower = Color.White;
                break;
            case 16:
                clNameColor = Color.FromArgb(255, 255, 0, 0);
                clNameColorLower = Color.White;
                break;
            case 17:
                clNameColor = Color.FromArgb(255, 232, 182, 149);
                clNameColorLower = Color.FromArgb(255, 122, 69, 26);
                break;
            case 18:
                clNameColor = Color.FromArgb(246, 245, 255);
                clNameColorLower = Color.FromArgb(125, 128, 137);
                break;
            case 19:
                clNameColor = Color.FromArgb(255, 238, 196, 85);
                clNameColorLower = Color.FromArgb(255, 255, 241, 200);
                break;
        }

        playerNameText.fillGradientMode = colorIndex > 11
            ? UiTextGradientMode.Vertical
            : UiTextGradientMode.None;
        playerNameText.fillGradientTopColor = clNameColor;
        playerNameText.fillGradientBottomColor = clNameColorLower;
        playerNameText.fillColor = clNameColor;
    }
}