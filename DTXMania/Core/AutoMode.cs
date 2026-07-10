namespace DTXMania.Core;

public class AutoMode
{
    public string label;
    public Dictionary<ELane, bool> state;
    public bool isCustom;

    public AutoMode(string label, Dictionary<ELane, bool> state, bool isCustom = false)
    {
        this.label = label;
        this.state = state;
        this.isCustom = isCustom;
    }
    
    public static readonly Dictionary<EInstrumentPart, List<AutoMode>> AutoModes = new()
    {
        {
            EInstrumentPart.DRUMS,
            [
                new AutoMode("Off", 
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, false},
                        {ELane.HH, false},
                        {ELane.LP, false},
                        {ELane.LBD, false},
                        {ELane.SD, false},
                        {ELane.BD, false},
                        {ELane.HT, false},
                        {ELane.LT, false},
                        {ELane.FT, false},
                        {ELane.CY, false},
                        {ELane.RD, false}
                    }),
                new AutoMode("All Auto", 
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, true},
                        {ELane.HH, true},
                        {ELane.LP, true},
                        {ELane.LBD, true},
                        {ELane.SD, true},
                        {ELane.BD, true},
                        {ELane.HT, true},
                        {ELane.LT, true},
                        {ELane.FT, true},
                        {ELane.CY, true},
                        {ELane.RD, true}
                    }),
                new AutoMode("Left Pedal",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, false},
                        {ELane.HH, false},
                        {ELane.LP, true},
                        {ELane.LBD, true},
                        {ELane.SD, false},
                        {ELane.BD, false},
                        {ELane.HT, false},
                        {ELane.LT, false},
                        {ELane.FT, false},
                        {ELane.CY, false},
                        {ELane.RD, false}
                    }),
                new AutoMode("Bass Pedal",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, false},
                        {ELane.HH, false},
                        {ELane.LP, false},
                        {ELane.LBD, false},
                        {ELane.SD, false},
                        {ELane.BD, true},
                        {ELane.HT, false},
                        {ELane.LT, false},
                        {ELane.FT, false},
                        {ELane.CY, false},
                        {ELane.RD, false}
                    }),
                new AutoMode("Both Pedals",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, false},
                        {ELane.HH, false},
                        {ELane.LP, true},
                        {ELane.LBD, true},
                        {ELane.SD, false},
                        {ELane.BD, true},
                        {ELane.HT, false},
                        {ELane.LT, false},
                        {ELane.FT, false},
                        {ELane.CY, false},
                        {ELane.RD, false}
                    }),
                new AutoMode("XG Lanes",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.LC, true},
                        {ELane.HH, false},
                        {ELane.LP, true},
                        {ELane.LBD, true},
                        {ELane.SD, false},
                        {ELane.BD, false},
                        {ELane.HT, false},
                        {ELane.LT, false},
                        {ELane.FT, true},
                        {ELane.CY, false},
                        {ELane.RD, false}
                    }),
                new AutoMode("Custom", new Dictionary<ELane, bool>(), isCustom: true)
            ]
        },
        {
            EInstrumentPart.GUITAR,
            [
                new AutoMode("Off",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.GtR, false},
                        {ELane.GtG, false},
                        {ELane.GtB, false},
                        {ELane.GtY, false},
                        {ELane.GtP, false},
                        {ELane.GtPick, false},
                        {ELane.GtW, false}
                    }),
                new AutoMode("All Auto",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.GtR, true},
                        {ELane.GtG, true},
                        {ELane.GtB, true},
                        {ELane.GtY, true},
                        {ELane.GtP, true},
                        {ELane.GtPick, true},
                        {ELane.GtW, true}
                    }),
                new AutoMode("Auto Neck",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.GtR, true},
                        {ELane.GtG, true},
                        {ELane.GtB, true},
                        {ELane.GtY, true},
                        {ELane.GtP, true},
                        {ELane.GtPick, false},
                        {ELane.GtW, false}
                    }),
                new AutoMode("Auto Pick",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.GtR, false},
                        {ELane.GtG, false},
                        {ELane.GtB, false},
                        {ELane.GtY, false},
                        {ELane.GtP, false},
                        {ELane.GtPick, true},
                        {ELane.GtW, false}
                    }),
                new AutoMode("Custom", new Dictionary<ELane, bool>(), isCustom: true)
            ]
        },
        {
            EInstrumentPart.BASS, 
            [
                new AutoMode("Off",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.BsR, false},
                        {ELane.BsG, false},
                        {ELane.BsB, false},
                        {ELane.BsY, false},
                        {ELane.BsP, false},
                        {ELane.BsPick, false},
                        {ELane.BsW, false}
                    }),
                new AutoMode("All Auto",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.BsR, true},
                        {ELane.BsG, true},
                        {ELane.BsB, true},
                        {ELane.BsY, true},
                        {ELane.BsP, true},
                        {ELane.BsPick, true},
                        {ELane.BsW, true}
                    }),
                new AutoMode("Auto Neck",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.BsR, true},
                        {ELane.BsG, true},
                        {ELane.BsB, true},
                        {ELane.BsY, true},
                        {ELane.BsP, true},
                        {ELane.BsPick, false},
                        {ELane.BsW, false}
                    }),
                new AutoMode("Auto Pick",
                    new Dictionary<ELane, bool>
                    {
                        {ELane.BsR, false},
                        {ELane.BsG, false},
                        {ELane.BsB, false},
                        {ELane.BsY, false},
                        {ELane.BsP, false},
                        {ELane.BsPick, true},
                        {ELane.BsW, false}
                    }),
                new AutoMode("Custom", new Dictionary<ELane, bool>(), isCustom: true)
            ]
        }
    };
}