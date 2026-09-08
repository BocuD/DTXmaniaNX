using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania;

public class DensityGraph : UIGroup
{
    [Themable] public EInstrumentPart instrument;

    //where the bars start, how far apart they sit and how big each one is. Drums and guitar draw a
    //different number of lanes over different art, so each has its own
    [Themable] public Vector2 drumBarOrigin = new(36, 284);
    [Themable] public float drumBarSpacing = 12.0f;
    [Themable] public Vector2 drumBarSize = new(4, 252);

    [Themable] public Vector2 guitarBarOrigin = new(34, 284);
    [Themable] public float guitarBarSpacing = 12.0f;
    [Themable] public Vector2 guitarBarSize = new(6, 252);

    [Themable] public float barMaxHeight = 252.0f;

    private readonly UIDataContext data = new();
    private readonly DensityGraphData values;

    //drawn per selection rather than authored, so they are built here and left out of the layout
    private UIImage[] drumBars = [];
    private UIImage[] guitarBars = [];

    private UIText? noteCountText;

    internal int noteCount;

    public DensityGraph() : this(EInstrumentPart.DRUMS)
    {
    }

    public DensityGraph(EInstrumentPart inst) : base("DensityGraph")
    {
        instrument = inst;
        pivot = new Vector2(0, 1);
        size = new Vector2(160, 342);

        values = new DensityGraphData(this);
        data.RegisterObject("Graph", () => values);
        dataContext = data;

        MakeComponent("DensityGraph", DensityGraphDefault);
    }

    protected override void OnContentLoaded()
    {
        noteCountText = GetChild<UIText>("NoteCount");

        if (instrument == EInstrumentPart.DRUMS)
        {
            drumBars = BuildBars("DrumBar", clDrumChipsBarColors, drumBarOrigin, drumBarSpacing, drumBarSize);
        }
        else
        {
            guitarBars = BuildBars("GuitarBar", clGBChipsBarColors, guitarBarOrigin, guitarBarSpacing, guitarBarSize);
        }
    }

    private UIImage[] BuildBars(string prefix, Color[] colors, Vector2 origin, float spacing, Vector2 barSize)
    {
        UIImage[] bars = new UIImage[colors.Length];

        for (int index = 0; index < colors.Length; index++)
        {
            bars[index] = AddChild(new UIImage
            {
                name = $"{prefix}{index}",
                imageSource = ImageSource.Solid,
                color = colors[index],
                pivot = new Vector2(0, 1),
                position = new Vector3(origin.X + index * spacing, origin.Y, 0),
                size = barSize,
                renderOrder = 2 + index,
                dontSerialize = true
            });
        }

        return bars;
    }

    private static UIGroup DensityGraphDefault()
    {
        UIGroup root = new("DensityGraph");

        Panel(root, "PanelDrums", @"Graphics\SongSelect\graph_panel_drums.png", new Vector2(160, 342),
            Vector3.Zero, -2, "Graph.IsDrums");
        Panel(root, "PanelGuitarBass", @"Graphics\SongSelect\graph_panel_guitarbass.png", new Vector2(116, 342),
            Vector3.Zero, -2, "Graph.IsGuitarBass");

        Panel(root, "ForegroundDrums", @"Graphics\SongSelect\graph_fg_drums.png", new Vector2(112, 286),
            new Vector3(30, 15, 0), -1, "Graph.IsDrums");
        Panel(root, "ForegroundGuitarBass", @"Graphics\SongSelect\graph_fg_guitarbass.png", new Vector2(74, 286),
            new Vector3(30, 15, 0), -1, "Graph.IsGuitarBass");

        UIText noteCount = root.AddChild(new UIText("", 16));
        noteCount.name = "NoteCount";
        noteCount.pivot = new Vector2(1, 1);
        noteCount.outlineWidth = 0;
        noteCount.position = new Vector3(150, 333, 0);
        noteCount.bindings.Add(new UIBinding("text", "Graph.NoteCount"));
        noteCount.bindings.Add(new UIBinding("position.X", "Graph.NoteCountX"));

        return root;
    }

    private static void Panel(UIGroup root, string name, string file, Vector2 size, Vector3 position,
        int renderOrder, string shownBy)
    {
        root.AddChild(new UIImage
        {
            name = name,
            imageSource = ImageSource.File,
            image = SkinResource.System(file),
            size = size,
            position = position,
            renderOrder = renderOrder,
            bindings = { new UIBinding("isVisible", shownBy) }
        });
    }

    [Themable] private Color[] clDrumChipsBarColors =
    [
        Color.Red,
        Color.DeepSkyBlue,
        Color.HotPink,
        Color.Yellow,
        Color.Green,
        Color.MediumPurple,
        Color.DarkRed,
        Color.Orange,
        Color.RoyalBlue
    ];
    
    [Themable] private Color[] clGBChipsBarColors =
    [
        Color.Red,
        Color.Green,
        Color.DeepSkyBlue,
        Color.Yellow,
        Color.HotPink,
        Color.White
    ];

    public void SelectionChanged(SongNode? song, CChartData? chart)
    {
        int nPanelNoteCount = 0;
        int[] arrChipsByLane = null;
        
        if (chart == null || !chart.HasChartForCurrentMode(true))
        {
            int count = (CDTXMania.GetCurrentInstrument() == 0) ? 9 : 6;
            arrChipsByLane = new int[count];
        }
        else
        {
            //drums
            if (CDTXMania.GetCurrentInstrument() == 0)
            {
                if (chart.SongInformation.chipCountByInstrument.Drums > 0)
                {
                    nPanelNoteCount = chart.SongInformation.chipCountByInstrument.Drums;
                    arrChipsByLane =
                    [
                        chart.SongInformation.chipCountByLane[ELane.LC],
                        chart.SongInformation.chipCountByLane[ELane.HH],
                        chart.SongInformation.chipCountByLane[ELane.LP],
                        chart.SongInformation.chipCountByLane[ELane.SD],
                        chart.SongInformation.chipCountByLane[ELane.HT],
                        chart.SongInformation.chipCountByLane[ELane.BD],
                        chart.SongInformation.chipCountByLane[ELane.LT],
                        chart.SongInformation.chipCountByLane[ELane.FT],
                        chart.SongInformation.chipCountByLane[ELane.CY]
                    ];
                }
            }
            else
            {
                if (CDTXMania.ConfigIni.bIsSwappedGuitarBass)
                {
                    if (chart.SongInformation.chipCountByInstrument.Bass > 0)
                    {
                        nPanelNoteCount = chart.SongInformation.chipCountByInstrument.Bass;
                        arrChipsByLane =
                        [
                            chart.SongInformation.chipCountByLane[ELane.BsR],
                            chart.SongInformation.chipCountByLane[ELane.BsG],
                            chart.SongInformation.chipCountByLane[ELane.BsB],
                            chart.SongInformation.chipCountByLane[ELane.BsY],
                            chart.SongInformation.chipCountByLane[ELane.BsP],
                            chart.SongInformation.chipCountByLane[ELane.BsPick]
                        ];
                    }
                }
                else
                {
                    if (chart.SongInformation.chipCountByInstrument.Guitar > 0)
                    {
                        nPanelNoteCount = chart.SongInformation.chipCountByInstrument.Guitar;
                        arrChipsByLane =
                        [
                            chart.SongInformation.chipCountByLane[ELane.GtR],
                            chart.SongInformation.chipCountByLane[ELane.GtG],
                            chart.SongInformation.chipCountByLane[ELane.GtB],
                            chart.SongInformation.chipCountByLane[ELane.GtY],
                            chart.SongInformation.chipCountByLane[ELane.GtP],
                            chart.SongInformation.chipCountByLane[ELane.GtPick]
                        ];
                    }
                }
            }
        }

        noteCount = nPanelNoteCount;

        //Draw Bar Graph for Chips per lane
        if (arrChipsByLane != null)
        {
            int nBarMaxHeight = (int)barMaxHeight;
            int[] chipsBarHeights = nCalculateChipsBarPxHeight(arrChipsByLane, nBarMaxHeight);

            if (CDTXMania.ConfigIni.bGuitarEnabled)
            {
                if (chipsBarHeights.Length == guitarBars.Length)
                {
                    for (int i = 0; i < guitarBars.Length; i++)
                    {
                        guitarBars[i].size.Y = chipsBarHeights[i];
                        //this.guitarBars[i].tDraw2D(CDTXMania.app.Device,
                        //    nGraphBaseX + 38 + i * 10, nGraphBaseY + 21 + (nBarMaxHeight - chipsBarHeights[i]), new Rectangle(0, 0, 4, chipsBarHeights[i]));
                    }
                }                        
            }
            else
            {
                if (chipsBarHeights.Length == drumBars.Length)
                {
                    for (int i = 0; i < drumBars.Length; i++)
                    {
                        drumBars[i].size.Y = chipsBarHeights[i];
                        //this.drumBars[i].tDraw2D(CDTXMania.app.Device,
                        //    nGraphBaseX + 31 + i * 8, nGraphBaseY + 21 + (nBarMaxHeight - chipsBarHeights[i]), new Rectangle(0, 0, 4, chipsBarHeights[i]));
                    }
                }
            }

        }

        // //Draw Progress Bar
        // tDrawProgressBar(strProgressText, nGraphBaseX + 18, nGraphBaseY + 21);
    }
    
    private int[] nCalculateChipsBarPxHeight(int[] arrChipCount, int nMaxBarLength)
    {
        if (arrChipCount != null)
        {
            int[] nChipsBarPxHeight = new int[arrChipCount.Length];

            //Official formula to compute bar Height is unknown (Need to RE)
            //Use a Placeholder formula for now
            //int nMaxFactor = nTotalNoteCount / arrChipCount.Length;
            int nMaxFactor = 300;
            //Capped by upper and lower bound
            //nMaxFactor = nMaxFactor < nLowerBound ? nLowerBound : nMaxFactor;
            //nMaxFactor = nMaxFactor > nUpperBound ? nUpperBound : nMaxFactor;

            for (int i = 0; i < nChipsBarPxHeight.Length; i++)
            {
                int nChipPxHeight = arrChipCount[i] * nMaxBarLength / nMaxFactor;
                nChipPxHeight = nChipPxHeight > nMaxBarLength ? nMaxBarLength : nChipPxHeight;
                nChipsBarPxHeight[i] = nChipPxHeight;
            }

            return nChipsBarPxHeight;
        }

        return null;
    }

    public override void DrawInspector()
    {
        base.DrawInspector();
        
        if (ImGui.CollapsingHeader("Density Graph"))
        {
            ImGui.Text($"Instrument: {instrument}");
            
            //inspector for colors
            if (instrument == EInstrumentPart.DRUMS)
            {
                for (int index = 0; index < clDrumChipsBarColors.Length; index++)
                {
                    if (Inspector.Inspect($"Drum Lane {Array.IndexOf(clDrumChipsBarColors, clDrumChipsBarColors[index])} Color", ref clDrumChipsBarColors[index]))
                    {
                        drumBars[index].color = clDrumChipsBarColors[index];
                    }
                }
            }
            else
            {
                for (int index = 0; index < clGBChipsBarColors.Length; index++)
                {
                    if (Inspector.Inspect($"Guitar/Bass Lane {Array.IndexOf(clGBChipsBarColors, clGBChipsBarColors[index])} Color", ref clGBChipsBarColors[index]))
                    {
                        guitarBars[index].color = clGBChipsBarColors[index];
                    }
                }
            }
        }
    }
}