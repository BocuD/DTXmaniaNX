using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

//Shared (STDGBVALUE-backed) options come from PlayerConfigPage
internal sealed class DrumsConfigPage : PlayerConfigPage
{
    private readonly DrumsLaneConfigPage lanes;
    private readonly DrumsDisplayConfigPage display;
    private readonly DrumsPadGroupConfigPage padGroups;
    private readonly DrumsHitSoundConfigPage hitSounds;
    private readonly DrumsAutoPlayConfigPage autoPlay;
    private readonly DrumsVelocityConfigPage velocity;

    public DrumsConfigPage(ConfigList list) : base(list, EPlayer.Drums)
    {
        lanes = new DrumsLaneConfigPage(list);
        display = new DrumsDisplayConfigPage(list);
        padGroups = new DrumsPadGroupConfigPage(list);
        hitSounds = new DrumsHitSoundConfigPage(list);
        autoPlay = new DrumsAutoPlayConfigPage(list);
        velocity = new DrumsVelocityConfigPage(list);
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        items.Add(BackItem());

        items.Add(CreateCardNameInputItem());
        items.Add(CreateGroupNameInputItem());

        items.Add(ScrollSpeedItem());
        items.Add(HidSudItem());
        items.Add(ReverseItem("判定ラインが上になり、ノーツが下から上へ流れます。", "Chips flow from the bottom to the top."));
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());
        items.Add(InputAdjustItem());

        items.Add(Toggle("HAZARD",
            "ドSハザードモード。\nGREAT以下の判定でも残り回数が減ります。",
            "Super Hazard mode.\nEven GREAT-or-lower judgements cost a life.",
            () => CDTXMania.ConfigIni.bHAZARD, v => CDTXMania.ConfigIni.bHAZARD = v));
        items.Add(Toggle("Tight", "チップのないところで叩くとミスになります。", "Hitting pad without chip is a MISS.",
            () => CDTXMania.ConfigIni.bTight, v => CDTXMania.ConfigIni.bTight = v));
        items.Add(MinComboItem("D-MinCombo", 1));

        items.Add(FolderItem("Lane Options",
            "レーンの種類と配置に関する項目を設定します。",
            "Open the lane layout sub menu.", lanes));
        items.Add(FolderItem("Display Options",
            "画面に表示する項目とエフェクトを設定します。",
            "Open the display and effect sub menu.", display));
        items.Add(FolderItem("Pad Grouping",
            "パッドの打ち分けを設定します。",
            "Open the pad grouping sub menu.", padGroups));
        items.Add(FolderItem("Hit Sound Options",
            "打撃音と発声音の決め方を設定します。",
            "Open the hit sound sub menu.", hitSounds));
        items.Add(FolderItem("Auto Play Options",
            "自動演奏とランダムに関する項目を設定します。",
            "Open the auto play and random sub menu.", autoPlay));

        items.Add(MonitorItem("DrumsMonitor"));

        items.Add(FolderItem("Drum Hit Velocity",
            "ドラムのヒットベロシティに関する項目を設定します。",
            "Settings for the drums hit velocity.", velocity));

        items.Add(FolderItem("Key Assignment",
            "ドラムのキー割り当てを設定します。",
            "Assign keys/pads for the drums.", KeyAssignPage.ForDrums(list)));

        return items;
    }
}
