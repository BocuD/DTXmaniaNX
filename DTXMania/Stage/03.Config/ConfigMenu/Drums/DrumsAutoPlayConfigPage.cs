using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class DrumsAutoPlayConfigPage : PlayerConfigPage
{
    public DrumsAutoPlayConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        List<(ELane, CItemToggle)> autoLanes =
        [
            (ELane.LC, Toggle("    LeftCymbal", "左シンバルを自動で演奏します。", "Play Left Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LC, v => CDTXMania.ConfigIni.bAutoPlay.LC = v)),
            (ELane.HH, Toggle("    HiHat", "ハイハットを自動で演奏します。", "Play HiHat automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.HH, v => CDTXMania.ConfigIni.bAutoPlay.HH = v)),
            (ELane.LP, Toggle("    LeftPedal", "左ペダルを自動で演奏します。", "Play Left Pedal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LP, v => CDTXMania.ConfigIni.bAutoPlay.LP = v)),
            (ELane.LBD, Toggle("    LBassDrum", "左バスドラムを自動で演奏します。", "Play Left Bass Drum automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LBD, v => CDTXMania.ConfigIni.bAutoPlay.LBD = v)),
            (ELane.SD, Toggle("    Snare", "スネアを自動で演奏します。", "Play Snare automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.SD, v => CDTXMania.ConfigIni.bAutoPlay.SD = v)),
            (ELane.BD, Toggle("    BassDrum", "バスドラムを自動で演奏します。", "Play Bass Drum automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.BD, v => CDTXMania.ConfigIni.bAutoPlay.BD = v)),
            (ELane.HT, Toggle("    HighTom", "ハイタムを自動で演奏します。", "Play High Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.HT, v => CDTXMania.ConfigIni.bAutoPlay.HT = v)),
            (ELane.LT, Toggle("    LowTom", "ロータムを自動で演奏します。", "Play Low Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.LT, v => CDTXMania.ConfigIni.bAutoPlay.LT = v)),
            (ELane.FT, Toggle("    FloorTom", "フロアタムを自動で演奏します。", "Play Floor Tom automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.FT, v => CDTXMania.ConfigIni.bAutoPlay.FT = v)),
            (ELane.CY, Toggle("    Cymbal", "右シンバルを自動で演奏します。", "Play Right Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.CY, v => CDTXMania.ConfigIni.bAutoPlay.CY = v)),
            (ELane.RD, Toggle("    Ride", "ライドシンバルを自動で演奏します。", "Play Ride Cymbal automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay.RD, v => CDTXMania.ConfigIni.bAutoPlay.RD = v)),
        ];
        AddAutoPlayBlock(items, autoLanes);

        items.Add(RandomItem());
        items.Add(RandomPedalItem());

        return items;
    }
}
