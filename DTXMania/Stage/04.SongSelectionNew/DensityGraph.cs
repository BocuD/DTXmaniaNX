using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using FDK;
using SharpDX;

namespace DTXMania;

public class DensityGraph : UIGroup
{
    private EInstrumentPart inst;
    
    public DensityGraph(EInstrumentPart inst) : base("DensityGraph")
    {
        this.inst = inst;

        switch (this.inst)
        {
            case EInstrumentPart.DRUMS:
                var graphPanel = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_panel_drums.png"))));
                graphPanel.position = new Vector3(0, 0, 0);
                graphPanel.renderOrder = 4;
                graphPanel.name = "GraphPanel";
        
                var graphFg = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_fg_drums.png"))));
                graphFg.position = new Vector3(30, 15, 0);
                graphFg.renderOrder = 5;
                graphFg.name = "GraphFg";
                break;
            
            case EInstrumentPart.GUITAR:
            case EInstrumentPart.BASS:
                var graphPanelGb = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_panel_guitarbass.png"))));
                graphPanelGb.position = new Vector3(0, 0, 0);
                graphPanelGb.renderOrder = 4;
                graphPanelGb.name = "GraphPanel";
        
                var graphFgGb = AddChild(new UIImage(DTXTexture.LoadFromPath(CSkin.Path(@"Graphics\SongSelect\graph_fg_guitarbass.png"))));
                graphFgGb.position = new Vector3(30, 15, 0);
                graphFgGb.renderOrder = 5;
                graphFgGb.name = "GraphFg";
                break;
        }
    }
    
    private CTexture[] txDrumChipsBarLine = new CTexture[9];
    private Color[] clDrumChipsBarColors =
    [
        Color.PaleVioletRed,
        Color.DeepSkyBlue,
        Color.HotPink,
        Color.Yellow,
        Color.Green,
        Color.MediumPurple,
        Color.Red,
        Color.Orange,
        Color.DeepSkyBlue
    ];
    private CTexture[] txGBChipsBarLine = new CTexture[6];
    private Color[] clGBChipsBarColors =
    [
        Color.Red,
        Color.Green,
        Color.DeepSkyBlue,
        Color.Yellow,
        Color.HotPink,
        Color.White
    ];

    public void SelectionChanged(SongNode? song, CScore? chart)
    {
        
    }
}