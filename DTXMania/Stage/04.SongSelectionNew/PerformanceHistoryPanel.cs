using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using DTXMania.UI.Text;

namespace DTXMania;

public class PerformanceHistoryPanel : ComponentInstance, IUIItemSource
{
    private const float RowSpacing = 20.0f;
    private const int RowCount = 5;

    //the visible part of 5_play history panel.png, which is 458x151 with the rest empty. Anchoring
    //measures from this box, so it has to be the art you can see
    private static readonly Vector2 PanelSize = new(345.0f, 130.0f);

    private CChartData? currentChart;
    private bool rowsDirty = true;

    private readonly PerformanceHistoryRowData[] rows = new PerformanceHistoryRowData[RowCount];
    private BaseTexture[]? rankIcons;

    public int ItemCount => RowCount;
    public object GetItem(int index) => rows[index];

    //the same icons the status pane uses, loaded lazily so deserialization does not touch the disk
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

    public PerformanceHistoryPanel() : base()
    {
        name = "PerformanceHistoryPanel";

        //on the instance, not on the BuildDefault root: EnsureContent only takes that tree's children
        size = PanelSize;
        Array.Fill(rows, PerformanceHistoryRowData.Empty);
    }

    public void SelectionChanged(CChartData? chart)
    {
        currentChart = chart;
        rowsDirty = true;
    }

    protected override void OnContentLoaded()
    {
        if (GetChild<UIItemsGroup>("Rows") is { } rowsGroup)
        {
            rowsGroup.itemDefault = BuildHistoryRowDefault;
            rowsGroup.SetSource(this);
        }

        rowsDirty = true;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        EnsureContent();

        //parsing and formatting five lines is selection-change work
        if (rowsDirty)
        {
            rowsDirty = false;
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = ResolveRow(i);
            }
        }

        base.Draw(parentMatrix);
    }

    private PerformanceHistoryRowData ResolveRow(int index)
    {
        if (currentChart == null)
        {
            return PerformanceHistoryRowData.Empty;
        }

        PerformanceHistoryLine line =
            PerformanceHistoryLine.TryRead(currentChart.SongInformation.PerformanceHistory[index]);

        if (line.Raw.Length == 0)
        {
            return PerformanceHistoryRowData.Empty;
        }

        if (line.Outcome == EPerformanceOutcome.Unknown)
        {
            return new PerformanceHistoryRowData { Raw = line.Raw, Date = line.Date };
        }

        return new PerformanceHistoryRowData
        {
            Raw = line.Raw,
            Date = line.Date,
            Outcome = line.Outcome.ToString(),
            Instrument = line.Instrument == EInstrumentPart.UNKNOWN
                ? string.Empty
                : line.Instrument.ToString(),
            Skill = Percent(line.Skill),
            Speed = line.Speed.Length > 0 ? $"x{line.Speed}" : string.Empty,
            Rank = line.Rank == (int)CScoreIni.ERANK.UNKNOWN ? null : RankIcons[line.Rank]
        };
    }

    //guitar and bass played together give two skills, and each is its own percentage
    private static string Percent(string skill)
    {
        if (skill.Length == 0)
        {
            return string.Empty;
        }

        return string.Join("/", skill.Split('/').Select(part => part.Trim() + "%"));
    }

    //the code default, also the seed for Components/PerformanceHistoryPanel.json
    protected override UIGroup BuildDefault()
    {
        UIGroup root = new("PerformanceHistoryPanel");

        root.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\5_play history panel.png"),
            renderOrder = 0
        });

        root.AddChild(new UIItemsGroup("Rows")
        {
            itemComponent = "Components/PerformanceHistoryRow.json",
            itemOffset = new Vector3(0.0f, RowSpacing, 0.0f),
            position = new Vector3(14.0f, 30.0f, 0.0f),
            renderOrder = 1
        });

        return root;
    }

    //the code default for one attempt, seeded into Components/PerformanceHistoryRow.json
    private static UIGroup BuildHistoryRowDefault()
    {
        UIGroup root = new("PerformanceHistoryRow");

        root.AddChild(new UIText(string.Empty, 15)
        {
            name = "Date",
            renderOrder = 1,
            outlineWidth = 0,
            bindings = { new UIBinding("text", "Item.Date") }
        });

        root.AddChild(new UIImage
        {
            name = "Rank",
            imageSource = ImageSource.Dynamic,
            dynamicSource = "Item.Rank",
            position = new Vector3(78.0f, -2.0f, 0.0f),
            size = new Vector2(20.0f, 20.0f),
            renderOrder = 1,
            isVisible = false,
            bindings = { new UIBinding("isVisible", "Item.HasRank") }
        });

        root.AddChild(new UIText(string.Empty, 15)
        {
            name = "Skill",
            position = new Vector3(98.0f, 0.0f, 0.0f),
            renderOrder = 1,
            outlineWidth = 0,
            isVisible = false,
            bindings =
            {
                new UIBinding("text", "Item.Skill"),
                new UIBinding("isVisible", "Item.HasSkill")
            }
        });

        root.AddChild(new UIText(string.Empty, 13)
        {
            name = "Instrument",
            position = new Vector3(152.0f, 1.0f, 0.0f),
            renderOrder = 1,
            outlineWidth = 0,
            bindings = { new UIBinding("text", "Item.Instrument") }
        });

        root.AddChild(new UIText(string.Empty, 13)
        {
            name = "Speed",
            position = new Vector3(200.0f, 1.0f, 0.0f),
            renderOrder = 1,
            outlineWidth = 0,
            isVisible = false,
            bindings =
            {
                new UIBinding("text", "Item.Speed"),
                new UIBinding("isVisible", "Item.HasSpeed")
            }
        });

        //an attempt that did not finish has no rank or skill to show, so the word carries the row
        root.AddChild(new UIText(string.Empty, 13)
        {
            name = "Outcome",
            position = new Vector3(98.0f, 1.0f, 0.0f),
            renderOrder = 1,
            outlineWidth = 0,
            bindings =
            {
                new UIBinding("text", "Item.Outcome"),
                new UIBinding("isVisible", "Item.HasSkill") { invert = true }
            }
        });

        //whatever a fork wrote that nothing above could read
        root.AddChild(new UIText(string.Empty, 13)
        {
            name = "Raw",
            renderOrder = 1,
            outlineWidth = 0,
            isVisible = false,
            bindings =
            {
                new UIBinding("text", "Item.Raw"),
                new UIBinding("isVisible", "Item.ShowRaw")
            }
        });

        return root;
    }
}
