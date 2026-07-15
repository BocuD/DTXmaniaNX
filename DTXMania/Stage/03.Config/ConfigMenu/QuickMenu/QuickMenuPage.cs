using DTXMania.Core;
using DTXMania.SongDb;
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

        //auto-play preset cycler (no per-lane toggles in the compact quick menu)
        items.Add(AutoPlaySelectorItem());
        
        CItemInteger iSystemRisky = new("Risky", 0, 10, CDTXMania.ConfigIni.nRisky,
            "設定した回数分\n" +
            "ミスをすると、強制的に\n"+
            "STAGE FAILEDになります。",
            "Risky mode:\nNumber of mistakes (Poor/Miss) before getting STAGE FAILED.\n"+
            "Set 0 to disable Risky mode.");
        iSystemRisky.BindConfig(
            () => iSystemRisky.nCurrentValue = CDTXMania.ConfigIni.nRisky,
            () => CDTXMania.ConfigIni.nRisky = iSystemRisky.nCurrentValue);
        items.Add(iSystemRisky);

        CItemInteger playSpeed = new("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, CDTXMania.ConfigIni.nPlaySpeed,
            "曲の演奏速度を、速くしたり\n遅くしたりすることができます。",
            "Change the song speed.\nFor example, set PlaySpeed = 0.500 for half speed.\nNote: It also changes the song's pitch.");
        playSpeed.BindConfig(
            () => playSpeed.nCurrentValue = CDTXMania.ConfigIni.nPlaySpeed,
            () => CDTXMania.ConfigIni.nPlaySpeed = playSpeed.nCurrentValue);
        
        //displayed as a decimal multiplier (value / 20), matching the old renderer's special case
        playSpeed.formatValue = () => (playSpeed.nCurrentValue / 20.0).ToString("0.000");
        items.Add(playSpeed);
        
        items.Add(ShutterInItem());
        items.Add(ShutterOutItem());
        
        CItemList dark = new("Dark", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDark[(int)instrument],
            "レーン表示オプションをまとめて切り替えます (HALF/FULL)。",
            "OFF: all shown. HALF: lanes/gauge hidden. FULL: also bar lines and hit bar hidden.",
            ["OFF", "HALF", "FULL"]);
        dark.action = () =>
        {
            if (dark.nCurrentlySelectedIndex == (int)EDarkMode.FULL)
            {
                CDTXMania.ConfigIni.nLaneDisp[(int)instrument] = 3;
                CDTXMania.ConfigIni.bJudgeLineDisp[(int)instrument] = false;
                CDTXMania.ConfigIni.bLaneFlush[(int)instrument] = false;
            }
            else if (dark.nCurrentlySelectedIndex == (int)EDarkMode.HALF)
            {
                CDTXMania.ConfigIni.nLaneDisp[(int)instrument] = 1;
                CDTXMania.ConfigIni.bJudgeLineDisp[(int)instrument] = true;
                CDTXMania.ConfigIni.bLaneFlush[(int)instrument] = true;
            }
            else
            {
                CDTXMania.ConfigIni.nLaneDisp[(int)instrument] = 0;
                CDTXMania.ConfigIni.bJudgeLineDisp[(int)instrument] = true;
                CDTXMania.ConfigIni.bLaneFlush[(int)instrument] = true;
            }
        };

        items.Add(dark);

        // var switcherButton = FolderItem($"Instrument: {part}",
        //     "設定する楽器を切り替えます。",
        //     "Switch which instrument these settings apply to.", switcher);
        // items.Add(switcherButton);

        var autoGhost = new CItemList("AUTO Ghost", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.eAutoGhost[(int)instrument],
            "AUTOプレーのゴーストを指定します。\n",
            "Specify Play Ghost data.\n",
            ["Perfect", "Last Play", "Hi Skill", "Hi Score"]);
        autoGhost.action = () =>
        {
            EAutoGhostData gd = (EAutoGhostData)autoGhost.nCurrentlySelectedIndex;
            CDTXMania.ConfigIni.eAutoGhost[(int)instrument] = gd;
        };
        items.Add(autoGhost);

        var targetGhost = new CItemList("Target Ghost", CItemBase.EPanelType.Normal,
            (int)CDTXMania.ConfigIni.eTargetGhost[(int)instrument],
            "ターゲットゴーストを指定します。\n",
            "Specify Target Ghost data.\n",
            ["None", "Perfect", "Last Play", "Hi Skill", "Hi Score"]);
        targetGhost.action = () =>
        {
            ETargetGhostData gtd = (ETargetGhostData)targetGhost.nCurrentlySelectedIndex;
            CDTXMania.ConfigIni.eTargetGhost[(int)instrument] = gtd;
        };
        items.Add(targetGhost);

        var fullConfig = new CItemBase("Open Full Config", CItemBase.EPanelType.Folder,
            "コンフィグ画面を開きます。",
            "Open the full configuration screen.")
        {
            action = () =>
            {
                GitaDoraTransition.Close(0,
                    () => CDTXMania.StageManager.tChangeStage(CDTXMania.StageManager.stageConfig));
            }
        };
        items.Add(fullConfig);
        
        items.Add(new CItemBase("Reload Songs", CItemBase.EPanelType.Normal,
            "曲データの一覧情報を取得し直します。\n新しい曲を追加したときや、曲データを\n更新したときに使用してください。",
            "Scan for new songs and update song list.")
        {
            action = () =>
            {
                if (CDTXMania.SongDb.status == SongDbScanStatus.Idle)
                {
                    GitaDoraTransition.Close(0, () =>
                    {
                        CDTXMania.SongDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload());
                    });
                }
            }
        });

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