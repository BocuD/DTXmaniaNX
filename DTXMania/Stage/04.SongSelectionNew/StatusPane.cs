using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;
using DTXMania.UI.Inspector;

namespace DTXMania;

/// <summary>Where one player's difficulty cursor sits on a pane, for the component to bind to.</summary>
public sealed class StatusPaneFrame
{
    [DataField] public bool Shown { get; internal set; }
    [DataField] public double Y { get; internal set; }

    [DataField] public bool ShowsLabel { get; internal set; }
}

public class StatusPane : UIGroup, IUIItemSource
{

    [AddChildMenu("Game/Song Select/Status Pane")]
    public new static UIDrawable Create() => new StatusPane();
    private const float VerticalSpacing = 74.0f;
    private const int DifficultyCount = 5;

    //assigned on selection change, never per frame, so the setter can drive a full row rebuild
    [SkinNonSerialized]
    public SongNode? song
    {
        get => currentSong;
        set
        {
            currentSong = value;
            rowsDirty = true;
        }
    }

    //serialized so a pane loaded from a skin layout knows which instrument it shows
    [Themable] public EInstrumentPart instrument;

    private SongNode? currentSong;
    private bool rowsDirty = true;

    private readonly ChartRowData[] rows = new ChartRowData[DifficultyCount];
    private BaseTexture[]? rankIcons;

    //a cursor per player that can be on this pane's part: the drums player or guitar 1, and guitar 2
    private readonly UIDataContext paneData = new();
    private readonly StatusPaneFrame frame = new();
    private readonly StatusPaneFrame frame2 = new();

    public int ItemCount => DifficultyCount;
    public object GetItem(int index) => rows[index];

    //loaded lazily so the parameterless deserialization ctor doesn't touch the disk
    private BaseTexture[] RankIcons
    {
        get
        {
            if (rankIcons == null)
            {
                rankIcons = new BaseTexture[7];
                for (int i = 0; i < rankIcons.Length; i++)
                {
                    rankIcons[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Rank\rank_{i}.png"));
                }
            }

            return rankIcons;
        }
    }

    public StatusPane() : base("StatusPane")
    {
        MakeComponent("StatusPane", StatusPaneDefault);
        Array.Fill(rows, ChartRowData.Empty);

        paneData.RegisterObject("Frame", () => frame);
        paneData.RegisterObject("Frame2", () => frame2);
        dataContext = paneData;
    }

    protected override void OnContentLoaded()
    {
        rowsDirty = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        //only on a selection change: resolving a row formats strings
        if (rowsDirty)
        {
            rowsDirty = false;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = ResolveRow(i);
            }
        }

        UpdateDifficultyFrame();

        base.Draw(parentMatrix);
    }

    private void UpdateDifficultyFrame()
    {
        CConfigIni config = CDTXMania.ConfigIni;
        int level = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSong);

        bool bothGuitars = config.IsPlayed(EPlayer.Guitar1) && config.IsPlayed(EPlayer.Guitar2);

        Place(frame, instrument == EInstrumentPart.DRUMS ? EPlayer.Drums : EPlayer.Guitar1, bothGuitars, level);
        Place(frame2, EPlayer.Guitar2, bothGuitars, level);
    }

    private void Place(StatusPaneFrame cursor, EPlayer player, bool bothGuitars, int level)
    {
        CConfigIni config = CDTXMania.ConfigIni;
        cursor.Shown = config.IsPlayed(player) && config.ChartFor(player) == (EChartPart)instrument;
        cursor.ShowsLabel = cursor.Shown && bothGuitars;
        cursor.Y = 5.0f - VerticalSpacing * level;
    }

    private ChartRowData ResolveRow(int difficulty)
    {
        SongNode? song = currentSong;

        bool filteredToOtherInstrument = song != null && song.filteredInstrumentPart != EInstrumentPart.UNKNOWN &&
                                         song.filteredInstrumentPart != instrument;

        if (song == null
            || song.nodeType != SongNode.ENodeType.SONG
            || song.charts[difficulty] == null
            || song.charts[difficulty].SongInformation.chipCountByInstrument[(int)instrument] == 0
            || filteredToOtherInstrument)
        {
            return ChartRowData.Empty;
        }

        CChartData chart = song.charts[difficulty];
        string level = $"{chart.SongInformation.GetLevel((int)instrument):0.00}";

        string name = song.difficultyLabel[difficulty] ?? string.Empty;
        bool customName = name.Length > 0 && !name.Equals(DifficultyLabel.Resolve(name, difficulty),
            StringComparison.CurrentCultureIgnoreCase);

        int rank = chart.SongInformation.BestRank[(int)instrument];
        if (rank == (int)CScoreIni.ERANK.UNKNOWN)
        {
            return new ChartRowData { Level = level, Name = name, HasCustomName = customName };
        }

        return new ChartRowData
        {
            Level = level,
            Name = name,
            HasCustomName = customName,
            Rank = RankIcons[rank],
            ShowSkill = chart.countSkill,
            Rate = $"{chart.SongInformation.HighCompletionRate[(int)instrument]:0.00}%"
        };
    }

    private static UIGroup StatusPaneDefault()
    {
        UIGroup root = new("StatusPane");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_difficulty_panel.png"),
            pivot = new Vector2(0.0f, 1.0f),
            renderOrder = 0
        });

        root.AddChild(PlayerCursor("Player2Cursor", "Frame2", new Color4(0x79 / 255f, 0xAA / 255f, 0xFF / 255f), "P2", new Vector3(210.0f, -3.0f, 0.0f), new Vector2(1.0f, 1.0f))).renderOrder = 3;
        root.AddChild(PlayerCursor("Player1Cursor", "Frame", Color4.White, "P1", new Vector3(2.0f, -93.0f, 0.0f), new Vector2(0.0f, 0.0f))).renderOrder = 4;

        root.AddChild(new UIItemsGroup("Rows", ChartRow)
        {
            itemOffset = new Vector3(0.0f, -VerticalSpacing, 0.0f),
            renderOrder = 2
        });

        return root;
    }
    private static UIGroup PlayerCursor(string name, string source, Color4 color, string label, Vector3 labelPosition, Vector2 labelPivot)
    {
        UIGroup root = new(name)
        {
            bindings =
            {
                new UIBinding("position.Y", source + ".Y"),
                new UIBinding("isVisible", source + ".Shown")
            }
        };

        root.AddChild(new UIImage
        {
            name = "Frame",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_difficultyframe.png"),
            pivot = new Vector2(0.0f, 1.0f),
            position = new Vector3(-7.0f, 0.0f, 0.0f),
            color = color
        });

        root.AddChild(new UIText(label, 18.0f)
        {
            name = "Label",
            font = SkinResource.System("texgyreadventor-bold.otf"),
            fillColor = color,
            outlineWidth = 2.0f,
            position = labelPosition,
            pivot = labelPivot,
            bindings = { new UIBinding("isVisible", source + ".ShowsLabel") }
        });

        return root;
    }

    private static UIGroup ChartRow()
    {
        UIGroup root = new("ChartRow");

        root.AddChild(new UIImage
        {
            name = "Skill",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\Rank\skill.png"),
            pivot = new Vector2(0.0f, 1.0f),
            position = new Vector3(14.0f, -49.0f, 0.0f),
            size = new Vector2(27.0f, 27.0f),
            renderOrder = 2,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.ShowSkill") }
        });

        root.AddChild(new UIImage
        {
            name = "Rank",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Item.Rank",
            pivot = new Vector2(0.0f, 1.0f),
            position = new Vector3(60.0f, -49.0f, 0.0f),
            size = new Vector2(27.0f, 27.0f),
            renderOrder = 2,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.HasRank") }
        });

        root.AddChild(new UINumberText("Item.Level")
        {
            name = "Level",
            position = new Vector3(125.0f, -41.0f, 0.0f),
            renderOrder = 3
        });

        root.AddChild(new UINumberText("Item.Rate", 0.6f)
        {
            name = "Rate",
            position = new Vector3(23.0f, -30.0f, 0.0f),
            renderOrder = 3,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.ShowRate") }
        });

        root.AddChild(new UIText(string.Empty, 15)
        {
            name = "Name",
            position = new Vector3(201.0f, -80.0f, 0.0f),
            pivot = new Vector2(1.0f, 0.0f),
            renderOrder = 1,
            outlineWidth = 0,
            style = UiTextStyle.Bold,
            isVisible = false,
            bindings =
            {
                new UIBinding("text", "Item.Name"),
                new UIBinding("isVisible", "Item.HasCustomName")
            }
        });

        return root;
    }
}
