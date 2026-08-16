using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Animation;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using FDK;
using Newtonsoft.Json.Linq;

namespace DTXMania;

/// <summary>
/// The settings the player can change without leaving song select. Opening and closing is an animation
/// clip rather than a counter in here, so where the list sits and how it arrives are both the skin's.
/// </summary>
public class QuickMenu : ComponentInstance
{
    private const string OpenClip = "open";
    private const string CloseClip = "close";

    private readonly CCommandHistory commandHistory = new();
    private readonly QuickMenuPage[] instruments = new QuickMenuPage[3];

    //both are built from what the player is playing rather than from the layout, so they are added here
    //and the clip addresses them by name
    private readonly ConfigList list;
    private readonly ConfigDescriptionPanel description;

    private bool isClosing;

    public QuickMenu() : base("Quick Menu")
    {
        list = AddChild(new ConfigList(20, 8));
        list.name = "List";
        list.onExitRoot = ToggleMenu;

        //position is relative to the centre-anchored menu
        description = AddChild(new ConfigDescriptionPanel());
        description.name = "Description";
        description.position = new Vector3(141 - 400, -138, 0);
        description.renderOrder = 1;

        QuickConfigInstrumentSwitcher instrumentSwitcher = new(list, instruments);
        instruments[0] = new QuickMenuPage(list, EInstrumentPart.DRUMS, instrumentSwitcher);
        instruments[1] = new QuickMenuPage(list, EInstrumentPart.GUITAR, instrumentSwitcher);
        instruments[2] = new QuickMenuPage(list, EInstrumentPart.BASS, instrumentSwitcher);

        list.SetItems(instruments[CDTXMania.GetCurrentInstrument()].Build());
    }

    /// <summary>The gesture that opens and closes the menu. Polled by the stage whatever holds focus,
    /// because it is a pad command rather than navigation — the list itself takes focus once open.</summary>
    public void PollToggleGesture()
    {
        if (CheckDoubleInput(EInstrumentPart.DRUMS, EPad.BD, EPadFlag.BD)
            || CheckDoubleInput(EInstrumentPart.GUITAR, EPad.P, EPadFlag.P)
            || CheckDoubleInput(EInstrumentPart.BASS, EPad.P, EPadFlag.P))
        {
            ToggleMenu();
        }

        description.Update(list.CurrentItem, isVisible && !isClosing && list.IsSettled);
    }

    public void ToggleMenu()
    {
        EnsureContent();
        CDTXMania.Skin.soundChange.tPlay();

        if (!isVisible)
        {
            isVisible = true;
            isClosing = false;
            list.SetItems(instruments[CDTXMania.GetCurrentInstrument()].Build(), 0);
            UIFocus.Push(list);
            animator?.Play(OpenClip);
            return;
        }

        //closing persists what was changed here: CommitPage already updated the in-memory ConfigIni, this
        //writes it out the way leaving the main config screen does
        isClosing = true;
        UIFocus.Pop(list);
        animator?.Play(CloseClip);

        CDTXMania.ConfigIni.tWrite(CDTXMania.executableDirectory + "Config.ini");
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        //the close animation decides when the menu is actually gone
        if (isClosing && animator is { isPlaying: false })
        {
            isClosing = false;
            isVisible = false;
        }

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

    //the code default, also the seed for Components/QuickMenu.json
    protected override UIGroup BuildDefault()
    {
        UIGroup root = new("QuickMenu");

        root.AddChild(new UIImage
        {
            name = "Dim",
            imageSource = ImageSource.Solid,
            color = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            size = new Vector2(1281, 721),
            anchor = new Vector2(0.5f, 0.5f),
            renderOrder = -1
        });

        //where the list ends up is the open clip's last keyframe, so a skin moves the menu by moving that
        root.animator = new Animator();
        root.animator.Add(Slide(OpenClip, from: -1000.0f, to: -600.0f, fadeTo: 0.8f));
        root.animator.Add(Slide(CloseClip, from: -600.0f, to: -1000.0f, fadeTo: 0.0f));

        return root;
    }

    private static AnimationClip Slide(string name, float from, float to, float fadeTo)
    {
        AnimationClip clip = new() { name = name, duration = Duration };

        clip.tracks.Add(Track("List/position.X", from, to));
        clip.tracks.Add(Track("List/position.Y", -200.0f, -200.0f));
        clip.tracks.Add(Track("Dim/color.Alpha", 0.8f - fadeTo, fadeTo));

        return clip;
    }

    private const float Duration = 0.1f;

    private static AnimationTrack Track(string path, float from, float to)
    {
        AnimationTrack track = new() { path = path };
        track.keyframes.Add(new Keyframe { time = 0.0f, rawValue = new JValue(from) });
        track.keyframes.Add(new Keyframe { time = Duration, rawValue = new JValue(to) });
        return track;
    }
}
