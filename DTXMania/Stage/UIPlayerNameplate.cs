using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
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
    
    private UIText playerNameText;
    private UIText titleText;

    private int instrument;

    public UIPlayerNameplate(int instrument = 0) : base("Nameplate")
    {
        this.instrument = instrument;
        
        titleText = AddChild(new UIText());
        titleText.name = "TitleText";
        titleText.position = new Vector3(6, 8, 0);

        playerNameText = AddChild(new UIText());
        playerNameText.name = "PlayerNameText";
        playerNameText.position = new Vector3(0, 26, 0);

        UpdateNameplate();
    }

    public void UpdateNameplate()
    {
        int colorIndex = CDTXMania.ConfigIni.nNameColor[instrument];

        string strPlayerName = string.IsNullOrEmpty(CDTXMania.ConfigIni.strCardName[instrument])
            ? "GUEST"
            : CDTXMania.ConfigIni.strCardName[0];
        string strTitleName = string.IsNullOrEmpty(CDTXMania.ConfigIni.strGroupName[instrument])
            ? ""
            : CDTXMania.ConfigIni.strGroupName[0];

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

        titleText.fontPath = UiFontDefaults.TryGetDefaultUiFontPath() ?? "";
        titleText.fontSize = 12;
        titleText.fillColor = Color.White;
        titleText.SetText(strTitleName);

        playerNameText.fontPath = UiFontDefaults.TryGetDefaultUiFontPath() ?? "";
        playerNameText.fontSize = 20;
        playerNameText.outlineWidth = 0;
        playerNameText.fillGradientMode = colorIndex > 11
            ? UiTextGradientMode.Vertical
            : UiTextGradientMode.None;
        playerNameText.fillGradientTopColor = clNameColor;
        playerNameText.fillGradientBottomColor = clNameColorLower;
        playerNameText.fillColor = clNameColor;
        playerNameText.SetText(strPlayerName);
    }
}