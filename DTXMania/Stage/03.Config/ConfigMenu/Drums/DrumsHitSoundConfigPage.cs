using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class DrumsHitSoundConfigPage : PlayerConfigPage
{
    public DrumsHitSoundConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(Toggle("HitSound", "打撃音の再生（ドラムのみ）。", "Play hitting chip sound (drums only).",
            () => CDTXMania.ConfigIni.bドラム打音を発声する, v => CDTXMania.ConfigIni.bドラム打音を発声する = v));
        items.Add(Toggle("Muting LP", "LP入力で発声中のHHを消音します。", "LP chips mute ringing HH chips.",
            () => CDTXMania.ConfigIni.bMutingLP, v => CDTXMania.ConfigIni.bMutingLP = v));

        items.Add(EnumChoice("HH Priority",
            "HHの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When HH lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityHH, v => CDTXMania.ConfigIni.eHitSoundPriorityHH = v));
        items.Add(EnumChoice("FT Priority",
            "FTの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When FT lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityFT, v => CDTXMania.ConfigIni.eHitSoundPriorityFT = v));
        items.Add(EnumChoice("CY Priority",
            "CYの打ち分け時、発声音の決め方。\nC>P: チップの音を優先。\nP>C: 叩いたパッドの音を優先。",
            "When CY lanes are grouped, which sound plays.\nC>P: the chip's sound wins.\nP>C: the hit pad's sound wins.",
            () => CDTXMania.ConfigIni.eHitSoundPriorityCY, v => CDTXMania.ConfigIni.eHitSoundPriorityCY = v));

        return items;
    }
}
