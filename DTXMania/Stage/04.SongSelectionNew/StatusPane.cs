using DTXMania.UI.Skin;
using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Text;

namespace DTXMania;

/// <summary>
/// One instrument's difficulty pane: background, difficulty frame and a row per difficulty. The rows are a
/// <see cref="UIItemsGroup"/> over five <see cref="ChartRowData"/>, so the pane supplies data and the
/// ChartRow component decides what a row looks like.
/// </summary>
public class StatusPane : ComponentInstance, IUIItemSource
{
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
    private UIImage? difficultyFrame;

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

    public StatusPane()
    {
        Array.Fill(rows, ChartRowData.Empty);
    }

    protected override void OnContentLoaded()
    {
        difficultyFrame = GetChild<UIImage>("DifficultyFrame");

        if (GetChild<UIItemsGroup>("Rows") is { } rowsGroup)
        {
            rowsGroup.itemDefault = BuildChartRowDefault;
            rowsGroup.SetSource(this);
        }

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
        if (difficultyFrame == null)
        {
            return;
        }

        //with a single guitar the frame belongs only to whichever of guitar/bass is being played
        difficultyFrame.isVisible = !CDTXMania.ConfigIni.bGuitarEnabled
                                    || !CDTXMania.ConfigIni.bSingleGuitar
                                    || instrument == (CDTXMania.ConfigIni.bIsSwappedGuitarBass ? EInstrumentPart.BASS : EInstrumentPart.GUITAR);

        int level = CDTXMania.StageManager.stageSongSelectionNew.GetClosestLevelToTargetForSong(currentSong);
        difficultyFrame.position = new Vector3(-7.0f, 5.0f - VerticalSpacing * level, 0.0f);
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

    //the code default, also the seed for Components/StatusPane.json
    protected override UIGroup BuildDefault()
    {
        UIGroup root = new("StatusPane");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_difficulty_panel.png"),
            anchor = new Vector2(0.0f, 1.0f),
            renderOrder = 0
        });

        root.AddChild(new UIImage
        {
            name = "DifficultyFrame",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_difficultyframe.png"),
            anchor = new Vector2(0.0f, 1.0f),
            position = new Vector3(-7.0f, 5.0f, 0.0f),
            renderOrder = 1
        });

        root.AddChild(new UIItemsGroup("Rows")
        {
            itemComponent = "Components/ChartRow.json",
            itemOffset = new Vector3(0.0f, -VerticalSpacing, 0.0f),
            renderOrder = 2
        });

        return root;
    }

    //the code default for one difficulty row, seeded into Components/ChartRow.json
    private static UIGroup BuildChartRowDefault()
    {
        UIGroup root = new("ChartRow");

        root.AddChild(new UIImage
        {
            name = "Skill",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\Rank\skill.png"),
            anchor = new Vector2(0.0f, 1.0f),
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
            anchor = new Vector2(0.0f, 1.0f),
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
            anchor = new Vector2(1.0f, 0.0f),
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
