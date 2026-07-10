using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

//per-pad minimum hit-velocity thresholds (0-100)
internal sealed class DrumsVelocityConfigPage : ConfigPage
{
    public DrumsVelocityConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items =
        [
            VelocityItem("Left cymbal", () => CDTXMania.ConfigIni.nVelocityMin.LC,
                v => CDTXMania.ConfigIni.nVelocityMin.LC = v),
            VelocityItem("Hi-hat", () => CDTXMania.ConfigIni.nVelocityMin.HH,
                v => CDTXMania.ConfigIni.nVelocityMin.HH = v),
            VelocityItem("Snare drum", () => CDTXMania.ConfigIni.nVelocityMin.SD,
                v => CDTXMania.ConfigIni.nVelocityMin.SD = v),
            VelocityItem("Bass drum", () => CDTXMania.ConfigIni.nVelocityMin.BD,
                v => CDTXMania.ConfigIni.nVelocityMin.BD = v),
            VelocityItem("High tom", () => CDTXMania.ConfigIni.nVelocityMin.HT,
                v => CDTXMania.ConfigIni.nVelocityMin.HT = v),
            VelocityItem("Low tom", () => CDTXMania.ConfigIni.nVelocityMin.LT,
                v => CDTXMania.ConfigIni.nVelocityMin.LT = v),
            VelocityItem("Floor tom", () => CDTXMania.ConfigIni.nVelocityMin.FT,
                v => CDTXMania.ConfigIni.nVelocityMin.FT = v),
            VelocityItem("Cymbal", () => CDTXMania.ConfigIni.nVelocityMin.CY,
                v => CDTXMania.ConfigIni.nVelocityMin.CY = v),
            VelocityItem("Ride cymbal", () => CDTXMania.ConfigIni.nVelocityMin.RD,
                v => CDTXMania.ConfigIni.nVelocityMin.RD = v),
            VelocityItem("Left pedal", () => CDTXMania.ConfigIni.nVelocityMin.LP,
                v => CDTXMania.ConfigIni.nVelocityMin.LP = v),
            VelocityItem("Left bass drum", () => CDTXMania.ConfigIni.nVelocityMin.LBD,
                v => CDTXMania.ConfigIni.nVelocityMin.LBD = v),
            BackItem()
        ];

        return items;
    }

    private static CItemInteger VelocityItem(string name, Func<int> get, Action<int> set)
    {
        CItemInteger item = new(name, 0, 100, get(),
            $"{name} ドラムの最小速度を調整します。\n0から100の範囲で選択できます。",
            $"Adjust the minimum hit velocity threshold of the {name}.\nA range from 0 to 100 can be selected.");
        item.BindConfig(
            () => item.nCurrentValue = get(),
            () => set(item.nCurrentValue));
        return item;
    }
}
