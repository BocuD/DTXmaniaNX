using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;

namespace DTXMania;

internal class QuickMenuPage(ConfigList list, EInstrumentPart part, QuickConfigInstrumentSwitcher switcher) : InstrumentConfigPage(list, part)
{
    public override List<CItemBase> Build()
    {
        var items = new List<CItemBase>();
        items.Add(ScrollSpeedItem());
        
        CItemInteger playSpeed = new("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, CDTXMania.ConfigIni.nPlaySpeed,
            "曲の演奏速度を、速くしたり\n遅くしたりすることができます。",
            "Change the song speed.\nFor example, set PlaySpeed = 0.500 for half speed.\nNote: It also changes the song's pitch.");
        playSpeed.BindConfig(
            () => playSpeed.nCurrentValue = CDTXMania.ConfigIni.nPlaySpeed,
            () => CDTXMania.ConfigIni.nPlaySpeed = playSpeed.nCurrentValue);
        // displayed as a decimal multiplier (value / 20), matching the old renderer's special case
        playSpeed.formatValue = () => (playSpeed.nCurrentValue / 20.0).ToString("0.000");
        items.Add(playSpeed);
        
        AddDisplayBlock(items);

        var switcherButton = FolderItem($"Instrument: {part}", "", "", switcher);
        items.Add(switcherButton);

        var fullConfig = new CItemBase("Open Full Config", CItemBase.EPanelType.Folder, "", "")
        {
            action = () =>
            {
                GitaDoraTransition.Close(0,
                    () => CDTXMania.StageManager.tChangeStage(CDTXMania.StageManager.stageConfig));
            }
        };
        items.Add(fullConfig);

        var closeButton = new CItemBase("Close", CItemBase.EPanelType.Return,
            "メニューを閉じる",
            "Close the quick config menu.")
        {
            action = list.Back
        };
        items.Add(closeButton);
        
        return items;
    }
}