using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Animation;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using FDK;
using Newtonsoft.Json.Linq;
using DTXMania.UI.Inspector;

namespace DTXMania;

/// <summary>
/// The settings the player can change without leaving song select. Opening and closing is an animation
/// clip rather than a counter in here, so where the list sits and how it arrives are both the skin's.
/// </summary>
public class QuickMenu : UIGroup
{

    [AddChildMenu("Game/Song Select/Quick Menu")]
    public new static UIDrawable Create() => new QuickMenu();
    private const string OpenClip = "open";
    private const string CloseClip = "close";

    private readonly CCommandHistory commandHistory = new();
    private readonly QuickMenuPage[] pages = new QuickMenuPage[3];

    //part of the component, so the clip addresses them by name and a skin can move them
    private ConfigList? list;
    private ConfigDescriptionPanel? description;
    private ConfigPlayfieldPreview? preview;
    private UIQuad? dim;

    private bool isClosing;

    public QuickMenu() : base("Quick Menu")
    {
        MakeComponent("QuickMenu", QuickMenuDefault);
    }

    //the pages are built around the list, so they wait until there is one
    protected override void OnContentLoaded()
    {
        list = FindChild<ConfigList>();
        description = FindChild<ConfigDescriptionPanel>();
        preview = FindChild<ConfigPlayfieldPreview>();
        dim = FindChild<UIQuad>();

        if (list == null)
        {
            return;
        }

        list.onExitRoot = ToggleMenu;

        pages[0] = new QuickMenuPage(list, EPlayer.Drums);
        pages[1] = new QuickMenuPage(list, EPlayer.Guitar1);
        pages[2] = new QuickMenuPage(list, EPlayer.Guitar2);

        list.OpenRoot(pages[(int)MenuPlayer]);
    }

    /// <summary>The gesture that opens and closes the menu. Polled by the stage whatever holds focus,
    /// because it is a pad command rather than navigation — the list itself takes focus once open.</summary>
    public void PollToggleGesture()
    {
        if (CheckDoubleInput(EPlayer.Drums, EPad.BD, EPadFlag.BD)
            || CheckDoubleInput(EPlayer.Guitar1, EPad.P, EPadFlag.P)
            || CheckDoubleInput(EPlayer.Guitar2, EPad.P, EPadFlag.P))
        {
            ToggleMenu();
        }

        if (list != null && description != null)
        {
            description.Update(list.CurrentItem, isVisible && !isClosing && list.IsSettled);
        }
    }

    public void ToggleMenu()
    {
        EnsureContent();

        //a layout without a list has no menu to open
        if (list == null)
        {
            return;
        }

        CDTXMania.Skin.soundChange.tPlay();

        if (!isVisible)
        {
            isVisible = true;
            isClosing = false;
            EPlayer player = MenuPlayer;
            list.OpenRoot(pages[(int)player]);

            if (preview != null)
            {
                preview.chartPath = SelectedChartPath();
                preview.Show(player);
            }

            UIFocus.Push(list);
            animator?.Play(OpenClip);
            return;
        }

        //closing persists what was changed here: CommitPage already updated the in-memory ConfigIni, this
        //writes it out the way leaving the main config screen does
        isClosing = true;
        UIFocus.Pop(list);
        preview?.Show(null);
        animator?.Play(CloseClip);

        CDTXMania.ConfigIni.tWrite(CDTXMania.executableDirectory + "Config.ini");
    }

    //the chart the player is looking at, or none when the cursor is on a folder
    private static string? SelectedChartPath()
    {
        CStageSongSelectionNew? select = CDTXMania.StageManager?.rCurrentStage as CStageSongSelectionNew;

        //ForCurrentInstrument is what deciding on a song does, so the preview plays what Enter would
        return select?.selectedChart?.ForCurrentInstrument().FileInformation.AbsoluteFilePath;
    }

    //what the canvas is wide decides whether the playfield has to move over at all. The menu hangs off
    //the left edge, so its own coordinates are canvas coordinates across
    private void LayoutForCanvas()
    {
        Vector2 canvas = UICanvas.canvasSize;
        float designLeft = (canvas.X - UICanvas.logicalSize.X) / 2f;

        if (dim != null)
        {
            dim.position = new Vector3(0, -canvas.Y / 2f, 0);
            dim.size = canvas;
        }

        if (preview == null)
        {
            return;
        }

        preview.position = new Vector3(designLeft, -UICanvas.logicalSize.Y / 2f, 0);

        //drums reach from 296, and the list ends where its rows end
        float listRight = (list != null ? list.position.X : 0) + ListWidth;
        float lanesLeft = designLeft + DrumsLanesLeft;
        preview.drumsX = MathF.Max(0, listRight + ListGap - lanesLeft);

        if (description != null)
        {
            description.position.X = lanesLeft + preview.drumsX - DescriptionWidth - ListGap;
        }
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();
        LayoutForCanvas();

        //the close animation decides when the menu is actually gone
        if (isClosing && animator is { isPlaying: false })
        {
            isClosing = false;
            isVisible = false;
        }

        base.Draw(parentMatrix);
    }

    public bool CheckDoubleInput(EPlayer player, EPad pad, EPadFlag flag)
    {
        if (CDTXMania.Pad.bPressed(player, pad))
        {
            commandHistory.Add(player, flag);
            EPadFlag[] comChangeScrollSpeed = [flag, flag];
            if (commandHistory.CheckCommand(comChangeScrollSpeed, player))
            {
                return true;
            }
        }

        return false;
    }

    private static UIGroup QuickMenuDefault()
    {
        UIGroup root = new("QuickMenu");

        ConfigList list = root.AddChild(new ConfigList(20, 8));
        list.name = "List";

        //drums are wide, so they sit right of the list. Guitar has room to be centred
        ConfigPlayfieldPreview preview = root.AddChild(new ConfigPlayfieldPreview());
        preview.name = "Preview";
        preview.drumsX = 190;
        preview.guitarX = 455;
        preview.renderOrder = -1;

        //position is relative to the centre-anchored menu
        ConfigDescriptionPanel description = root.AddChild(new ConfigDescriptionPanel());
        description.name = "Description";
        description.position = new Vector3(141 - 400, -138, 0);
        description.renderOrder = 1;

        root.AddChild(new UIQuad
        {
            name = "Dim",
            color = Color4.Black,
            renderOrder = -2
        });

        //where the list ends up is the open clip's last keyframe, so a skin moves the menu by moving that
        root.animator = new Animator();
        root.animator.Add(Slide(OpenClip, from: -400.0f, to: 40.0f, fadeTo: 0.8f));
        root.animator.Add(Slide(CloseClip, from: 40.0f, to: -400.0f, fadeTo: 0.0f));

        return root;
    }

    private static AnimationClip Slide(string name, float from, float to, float fadeTo)
    {
        AnimationClip clip = new() { name = name, duration = Duration };

        clip.tracks.Add(Track("List/position.X", from, to));
        clip.tracks.Add(Track("List/position.Y", -200.0f, -200.0f));
        clip.tracks.Add(Track("Dim/color.Alpha", 0.8f - fadeTo, fadeTo));
        clip.tracks.Add(Track("List/tint.Alpha", 0, 1));

        return clip;
    }

    private const float Duration = 0.1f;

    private const float ListWidth = 437;
    private const float ListGap = 20;
    private const float DrumsLanesLeft = 296;
    private const float DescriptionWidth = 280;

    private static AnimationTrack Track(string path, float from, float to)
    {
        AnimationTrack track = new() { path = path };
        track.keyframes.Add(new Keyframe { time = 0.0f, rawValue = new JValue(from) });
        track.keyframes.Add(new Keyframe { time = Duration, rawValue = new JValue(to) });
        return track;
    }
}
