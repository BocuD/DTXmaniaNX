using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class GuitarStyleConfigPage : PlayerConfigPage
{
    private static readonly string[] LaneNames = ["R", "G", "B", "Y", "P", "Pick", "Wailing"];

    private static readonly string[] LaneNamesJp =
        ["Rネック", "Gネック", "Bネック", "Yネック", "Pネック", "ピック", "ウェイリング"];

    private readonly GuitarLaneConfigPage lanes;
    private readonly GuitarDisplayConfigPage display;

    public GuitarStyleConfigPage(ConfigList list, EPlayer player) : base(list, player)
    {
        lanes = new GuitarLaneConfigPage(list, player);
        display = new GuitarDisplayConfigPage(list, player);
    }

    private bool IsGuitar => player == EPlayer.Guitar1;

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(CreateCardNameInputItem());
        items.Add(CreateGroupNameInputItem());

        items.Add(ScrollSpeedItem());
        items.Add(HidSudItem());
        items.Add(ReverseItem(IsGuitar ? "ギターチップが上から下に流れます。" : "ベースチップが上から下に流れます。",
            "Chips flow from the top to the bottom."));
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());
        items.Add(InputAdjustItem());

        items.Add(LightItem());
        items.Add(PerformanceModeItem());
        items.Add(MinComboItem(IsGuitar ? "G-MinCombo" : "B-MinCombo", 0));

        items.Add(RandomItem());
        AddAutoPlayBlock(items, AutoLanes());

        items.Add(FolderItem("Lane Options",
            "レーンの配置に関する項目を設定します。",
            "Open the lane layout sub menu.", lanes));
        items.Add(FolderItem("Display Options",
            "画面に表示する項目を設定します。",
            "Open the display sub menu.", display));

        items.Add(FolderItem("Key Assignment",
            IsGuitar ? "ギターのキー割り当てを設定します。" : "ベースのキー割り当てを設定します。",
            IsGuitar ? "Assign keys/pads for the guitar." : "Assign keys/pads for the bass.",
            IsGuitar ? KeyAssignPage.ForGuitar(list) : KeyAssignPage.ForBass(list)));

        return items;
    }

    private List<(ELane, CItemToggle)> AutoLanes()
    {
        ELane first = IsGuitar ? ELane.GtR : ELane.BsR;
        List<(ELane, CItemToggle)> autoLanes = [];

        for (int i = 0; i < LaneNames.Length; i++)
        {
            ELane lane = first + i;
            autoLanes.Add((lane, Toggle($"    {LaneNames[i]}",
                $"{LaneNamesJp[i]}を自動で演奏します。",
                $"Play {LaneNames[i]} automatically.",
                () => CDTXMania.ConfigIni.bAutoPlay[(int)lane],
                v => CDTXMania.ConfigIni.bAutoPlay[(int)lane] = v)));
        }

        return autoLanes;
    }
}
