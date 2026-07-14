using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

public class QuickMenu : UIGroup
{
    private readonly ConfigList list;
    
    private CStageConfig.STKeyRepetitionCounter ctKeyRepetition;
    private CCommandHistory commandHistory;

    private UIImage blackTexture;
    private ConfigDescriptionPanel descriptionPanel;

    private CCounter openCounter;

    private QuickMenuPage[] instruments;

    public QuickMenu() : base("Quick Menu")
    {
        commandHistory = new CCommandHistory();
        for (int i = 0; i < 4; i++)
        {
            ctKeyRepetition[i] = new CCounter(0, 0, 0, CDTXMania.Timer);
        }

        blackTexture = AddChild(new UIImage(BaseTexture.CreateSolidColor(Color4.Black)));
        blackTexture.size = new Vector2(1281, 721);
        blackTexture.anchor = new Vector2(0.5f, 0.5f);
        blackTexture.color = new Color4(0, 0, 0, 0);
        
        openCounter = new CCounter();
        
        list = AddChild(new ConfigList(20, 8));
        list.onExitRoot = ToggleMenu;
        list.position.Y = -200;

        instruments = new QuickMenuPage[3];
        QuickConfigInstrumentSwitcher instrumentSwitcher = new(list, instruments);
        instruments[0] = new QuickMenuPage(list, EInstrumentPart.DRUMS, instrumentSwitcher);
        instruments[1] = new QuickMenuPage(list, EInstrumentPart.GUITAR, instrumentSwitcher);
        instruments[2] = new QuickMenuPage(list, EInstrumentPart.BASS, instrumentSwitcher);
        
        list.SetItems(instruments[CDTXMania.GetCurrentInstrument()].Build());
        list.SetFocused(true);

        //help-text panel on the right (position is relative to the centre-anchored menu)
        descriptionPanel = AddChild(new ConfigDescriptionPanel());
        descriptionPanel.position = new Vector3(141 - 400, -138, 0);
        descriptionPanel.renderOrder = 1;
    }

    public void HandleNavigation()
    {
        if (CheckDoubleInput(EInstrumentPart.DRUMS, EPad.BD, EPadFlag.BD))
        {
            ToggleMenu();
        }

        if (CheckDoubleInput(EInstrumentPart.GUITAR, EPad.P, EPadFlag.P))
        {
            ToggleMenu();
        }

        if (CheckDoubleInput(EInstrumentPart.BASS, EPad.P, EPadFlag.P))
        {
            ToggleMenu();
        }

        if (isVisible)
        {
            if (CDTXMania.Input.ActionCancel())
            {
                CDTXMania.Skin.soundCancel.tPlay();
                list.Cancel();
            }
            else if (CDTXMania.Input.ActionDecide())
            {
                list.Confirm();
            }

            ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDX.DirectInput.Key.UpArrow),
                () => list.MoveUp());
            ctKeyRepetition.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R),
                () => list.MoveUp(), 400, 25);
            if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
                list.MoveUp();
            
            ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDX.DirectInput.Key.DownArrow),
                () => list.MoveDown());
            ctKeyRepetition.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), 
                () => list.MoveDown(), 400, 25);
            if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
                list.MoveDown();
        }

        descriptionPanel.Update(list.CurrentItem, isVisible && !isClosing && list.IsSettled);
    }

    private bool isClosing = false;
    public void ToggleMenu()
    {
        CDTXMania.Skin.soundChange.tPlay();

        openCounter.tStart(0, 100, 1, CDTXMania.Timer);
        
        if (!isVisible)
        {
            //open
            isVisible = true;
            isClosing = false;
            list.SetItems(instruments[CDTXMania.GetCurrentInstrument()].Build(), 0);
        }
        else
        {
            //close
            isClosing = true;

            //commit
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        openCounter.tUpdate();
        var alpha = openCounter.nCurrentValue / (float)openCounter.nEndValue;
        
        if (isClosing) alpha = 1.0f - alpha;
        blackTexture.color.Alpha = alpha * 0.8f;

        if (isClosing && alpha == 0) isVisible = false;
        
        list.position.X = (alpha * 400) - 1000;
        
        base.Draw(parentMatrix);
    }

    public bool CheckDoubleInput(EInstrumentPart part, EPad pad, EPadFlag flag)
    {
        if (CDTXMania.Pad.bPressed(part, pad))
        {
            commandHistory.Add(part, flag);
            EPadFlag[] comChangeScrollSpeed = [flag, flag];
            if (commandHistory.CheckCommand(comChangeScrollSpeed, part))
            {
                return true;
            }
        }

        return false;
    }
}