using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.SongDb;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania;

public class DensityGraph : UIGroup
{
    private EInstrumentPart inst;

    private UIText noteCountText;
    
    public DensityGraph(EInstrumentPart inst) : base("DensityGraph")
    {
        this.inst = inst;

        noteCountText = AddChild(new UIText("", 16));
        noteCountText.anchor = new Vector2(1, 1);
        noteCountText.outlineWidth = 0;
        
        var white = BaseTexture.CreateSolidColor(Color4.White);
        
        switch (this.inst)
        {
            case EInstrumentPart.DRUMS:
                noteCountText.position = new Vector3(150, 333, 0);

                var graphPanel = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_panel_drums.png"))));
                graphPanel.position = new Vector3(0, 0, 0);
                graphPanel.renderOrder = -2;
                graphPanel.name = "GraphPanel";
                size = graphPanel.size;
        
                var graphFg = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_fg_drums.png"))));
                graphFg.position = new Vector3(30, 15, 0);
                graphFg.renderOrder = -1;
                graphFg.name = "GraphFg";
                
                for (int index = 0; index < clDrumChipsBarColors.Length; index++)
                {
                    Color c = clDrumChipsBarColors[index]; 
                    drumChipsBarLine[index] = AddChild(new UIImage(white));
                    drumChipsBarLine[index].color = c;
                    drumChipsBarLine[index].anchor = new Vector2(0, 1);
                    drumChipsBarLine[index].position = new Vector3(36 + index * 12, 284, 0);
                    drumChipsBarLine[index].size = new Vector2(4, 252);
                    drumChipsBarLine[index].renderOrder = 2 + index;
                }

                break;
            
            case EInstrumentPart.GUITAR:
            case EInstrumentPart.BASS:
                noteCountText.position = new Vector3(102, 333, 0);

                var graphPanelGb = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_panel_guitarbass.png"))));
                graphPanelGb.position = new Vector3(0, 0, 0);
                graphPanelGb.renderOrder = -2;
                graphPanelGb.name = "GraphPanel";
                size = graphPanelGb.size;
        
                var graphFgGb = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_fg_guitarbass.png"))));
                graphFgGb.position = new Vector3(30, 15, 0);
                graphFgGb.renderOrder = -1;
                graphFgGb.name = "GraphFg";
                
                for (int index = 0; index < clGBChipsBarColors.Length; index++)
                {
                    Color c = clGBChipsBarColors[index]; 
                    gbChipsBarLine[index] = AddChild(new UIImage(white));
                    gbChipsBarLine[index].color = c;
                    gbChipsBarLine[index].anchor = new Vector2(0, 1);
                    gbChipsBarLine[index].position = new Vector3(34 + index * 12, 284, 0);
                    gbChipsBarLine[index].size = new Vector2(6, 252);
                    gbChipsBarLine[index].renderOrder = 2 + index;
                }
                break;
        }

        anchor = new Vector2(0, 1);
    }
    
    private UIImage[] drumChipsBarLine = new UIImage[9];
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
    
    private UIImage[] gbChipsBarLine = new UIImage[6];
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

        //Draw total notes
        noteCountText.SetText(nPanelNoteCount > 0 ? nPanelNoteCount.ToString() : "");

        //Draw Bar Graph for Chips per lane
        if (arrChipsByLane != null)
        {
            int nBarMaxHeight = 252;
            int[] chipsBarHeights = nCalculateChipsBarPxHeight(arrChipsByLane, nBarMaxHeight);

            if (CDTXMania.ConfigIni.bGuitarEnabled)
            {
                if (chipsBarHeights.Length == gbChipsBarLine.Length)
                {
                    for (int i = 0; i < gbChipsBarLine.Length; i++)
                    {
                        gbChipsBarLine[i].size.Y = chipsBarHeights[i];
                        //this.gbChipsBarLine[i].tDraw2D(CDTXMania.app.Device,
                        //    nGraphBaseX + 38 + i * 10, nGraphBaseY + 21 + (nBarMaxHeight - chipsBarHeights[i]), new Rectangle(0, 0, 4, chipsBarHeights[i]));
                    }
                }                        
            }
            else
            {
                if (chipsBarHeights.Length == drumChipsBarLine.Length)
                {
                    for (int i = 0; i < drumChipsBarLine.Length; i++)
                    {
                        drumChipsBarLine[i].size.Y = chipsBarHeights[i];
                        //this.drumChipsBarLine[i].tDraw2D(CDTXMania.app.Device,
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
            ImGui.Text($"Instrument: {inst}");
            
            //inspector for colors
            if (inst == 0)
            {
                for (int index = 0; index < clDrumChipsBarColors.Length; index++)
                {
                    if (Inspector.Inspect($"Drum Lane {Array.IndexOf(clDrumChipsBarColors, clDrumChipsBarColors[index])} Color", ref clDrumChipsBarColors[index]))
                    {
                        drumChipsBarLine[index].color = clDrumChipsBarColors[index];
                    }
                }
            }
            else
            {
                for (int index = 0; index < clGBChipsBarColors.Length; index++)
                {
                    if (Inspector.Inspect($"Guitar/Bass Lane {Array.IndexOf(clGBChipsBarColors, clGBChipsBarColors[index])} Color", ref clGBChipsBarColors[index]))
                    {
                        gbChipsBarLine[index].color = clGBChipsBarColors[index];
                    }
                }
            }
        }
    }
}