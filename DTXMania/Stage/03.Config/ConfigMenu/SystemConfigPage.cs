using System.Diagnostics;
using System.Windows.Forms;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class SystemConfigPage : ConfigPage
{
    private readonly ConfigPage graphics;
    private readonly ConfigPage skin;
    private readonly ConfigPage audio;
    private readonly ConfigPage gameplay;
    private readonly ConfigPage menu;

    public SystemConfigPage(ConfigList list, ConfigPage graphics, ConfigPage skin, ConfigPage audio,
        ConfigPage gameplay, ConfigPage menu) : base(list)
    {
        this.graphics = graphics;
        this.skin = skin;
        this.audio = audio;
        this.gameplay = gameplay;
        this.menu = menu;
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        // top "<< Back" returns to the left menu (root page)
        items.Add(BackItem());

        items.Add(FolderItem("Graphics Options",
            "システムのグラフィック設定に関する項目を設定します。",
            "Open the graphics settings sub menu.", graphics));
        items.Add(FolderItem("Skin Options",
            "システムのテーマ設定に関する項目を設定します。",
            "Open the skin settings sub menu.", skin));
        items.Add(FolderItem("Audio Options",
            "システムのオーディオ設定に関する項目を設定します。",
            "Open the audio settings sub menu.", audio));
        items.Add(FolderItem("Gameplay Options",
            "ゲーム設定に関する項目を設定します。",
            "Open the gameplay settings sub menu.", gameplay));
        items.Add(FolderItem("Menu Options",
            "メニュー設定に関する項目を設定します。",
            "Open the menu settings sub menu.", menu));

        items.Add(new CItemBase("Reload Songs", CItemBase.EPanelType.Normal,
            "曲データの一覧情報を取得し直します。\n新しい曲を追加したときや、曲データを\n更新したときに使用してください。",
            "Scan for new songs and update song list.")
        {
            action = () =>
            {
                if (CDTXMania.SongDb.status == SongDbScanStatus.Idle)
                {
                    CDTXMania.SongDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload());
                }
            }
        });

        items.Add(new CItemBase("Reload Songs (Full)", CItemBase.EPanelType.Normal,
            "曲データのキャッシュをクリアして、曲データを完全に再読み込みします。",
            "Clear song data cache and perform full reload of song data.")
        {
            action = () =>
            {
                if (CDTXMania.SongDb.status == SongDbScanStatus.Idle)
                {
                    CDTXMania.SongDb.ClearCache();
                    CDTXMania.SongDb.StartScan(() => CDTXMania.StageManager.stageSongSelectionNew.Reload());
                }
            }
        });

        // Game selection (drums / 1P guitar / 2P guitar). The old list wrote the three flags in a
        // special case of tRecordToConfigIni; here the write closure does it directly.
        int gameMode = CDTXMania.ConfigIni.bDrumsEnabled ? 0 : CDTXMania.ConfigIni.bSingleGuitar ? 1 : 2;
        CItemList gameSelection = new("Game Selection", CItemBase.EPanelType.Normal, gameMode,
            "使用楽器の選択：\nDrums: ドラムのみ有効にします。\n1 Player Guitar: ギターのみの専用画面を\n用います。\n2 Player Guitar: ベースとギターを\n同時演奏する専用画面を用います。",
            "Instrument selection:\nDrums: Play the drums.\n1 Player Guitar: Play guitar.\n2 Player Guitar: Multiplayer guitar.",
            ["Drums", "1 Player Guitar", "2 Player Guitar"]);
        gameSelection.BindConfig(
            () => gameSelection.nCurrentlySelectedIndex =
                CDTXMania.ConfigIni.bDrumsEnabled ? 0 : CDTXMania.ConfigIni.bSingleGuitar ? 1 : 2,
            () =>
            {
                CDTXMania.ConfigIni.bDrumsEnabled = gameSelection.nCurrentlySelectedIndex == 0;
                CDTXMania.ConfigIni.bGuitarEnabled = gameSelection.nCurrentlySelectedIndex > 0;
                CDTXMania.ConfigIni.bSingleGuitar = gameSelection.nCurrentlySelectedIndex == 1;
            });
        items.Add(gameSelection);

        CItemList infoType = new("InfoType", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nInfoType,
            "Helpボタンを押した時に出る\n情報表示を変更できます。\nType-A FPS、BGMアジャスト\nなどの情報が出ます。\nType-B 判定数などが出ます。\n",
            "Type-A: FPS, BGM adjustment are display\nType-B: Number of perfect/great etc. skill rate are displayed.",
            ["Type-A", "Type-B"]);
        infoType.BindConfig(
            () => infoType.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nInfoType,
            () => CDTXMania.ConfigIni.nInfoType = infoType.nCurrentlySelectedIndex);
        items.Add(infoType);

        CItemToggle bufferedInput = new("BufferedInput", CDTXMania.ConfigIni.bBufferedInput,
            "バッファ入力モード：\nON にすると、FPS を超える入力解像\n度を実現します。\nOFF にすると、入力解像度は FPS に\n等しくなります。",
            "Select joystick/keyboard/\nmouse input buffer mode.\nON to use buffer input. No lost/lags.\nOFF to use realtime input. May cause lost/lags for input. Input frequency is synchronized with FPS.");
        bufferedInput.BindConfig(
            () => bufferedInput.bON = CDTXMania.ConfigIni.bBufferedInput,
            () => CDTXMania.ConfigIni.bBufferedInput = bufferedInput.bON);
        items.Add(bufferedInput);

        CItemToggle debugInfo = new("Debug Info", CDTXMania.ConfigIni.bShowPerformanceInformation,
            "演奏情報の表示：\n演奏中、BGA領域の下部に\n演奏情報を表示します。\nまた、小節線の横に\n小節番号が表示されるように\nなります。",
            "Show song information on playing BGA area (FPS, BPM, total time etc)\nYou can turn ON/OFF the indications by pushing [Del] while playing.");
        debugInfo.BindConfig(
            () => debugInfo.bON = CDTXMania.ConfigIni.bShowPerformanceInformation,
            () => CDTXMania.ConfigIni.bShowPerformanceInformation = debugInfo.bON);
        items.Add(debugInfo);

        CItemToggle traceLog = new("TraceLog", CDTXMania.ConfigIni.bOutputLogs,
            "Traceログ出力：\nDTXManiaLog.txt にログを出力します。\n変更した場合は、DTXMania の再起動\n後に有効となります。",
            "Turn ON to output debug logs to DTXManiaLog.txt file\nEffective after next DTXMania restart.");
        traceLog.BindConfig(
            () => traceLog.bON = CDTXMania.ConfigIni.bOutputLogs,
            () => CDTXMania.ConfigIni.bOutputLogs = traceLog.bON);
        items.Add(traceLog);

        CItemList chipTiming = new("Chip Timing Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            "発声時刻の計算方式を選択します。\nOriginal: 従来のDTXManiaと互換の計算方式\nAccurate: BPM/小節長変更による時刻のずれを補正\n※BPMや小節長の変更が多い曲以外では違いはほぼありません。",
            "Select Chip Timing Mode:\nOriginal: Compatible with other DTXMania players\nAccurate: Fixes time loss from BPM/Bar-Length changes\nNote: Only songs with many BPM/Bar-Length changes are noticeably affected.",
            ["Original", "Accurate"]);
        chipTiming.BindConfig(
            () => chipTiming.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            () => CDTXMania.ConfigIni.nChipPlayTimeComputeMode = chipTiming.nCurrentlySelectedIndex);
        items.Add(chipTiming);

        CItemToggle discord = new("Discord Integration", CDTXMania.ConfigIni.bDiscordRichPresenceEnabled,
            "Discord Rich Presence：\nDiscordのステータスを更新します。\nONにすると、現在の曲名や\nプレイ中のモードなどが表示されます。",
            "Enable Discord Rich Presence to update your Discord status with current song and playing mode.");
        discord.BindConfig(
            () => discord.bON = CDTXMania.ConfigIni.bDiscordRichPresenceEnabled,
            () =>
            {
                bool wasEnabled = CDTXMania.ConfigIni.bDiscordRichPresenceEnabled;
                CDTXMania.ConfigIni.bDiscordRichPresenceEnabled = discord.bON;

                if (!discord.bON && wasEnabled)
                {
                    CDTXMania.DiscordRichPresence?.Dispose();
                    CDTXMania.DiscordRichPresence = null;
                }

                if (discord.bON && !wasEnabled)
                {
                    CDTXMania.DiscordRichPresence = new CDiscordRichPresence(CDTXMania.ConfigIni.strDiscordRichPresenceApplicationID);
                }
            });
        items.Add(discord);

        items.Add(FolderItem("Key Assignment",
            "システムキーの割り当てを設定します。",
            "Assign keys/pads for system actions.", KeyAssignPage.ForSystem(list)));

        items.Add(new CItemBase("Import Config", CItemBase.EPanelType.Normal,
            "config.iniファイルから設定を再読み込みする。",
            "Import and apply settings from an external config.ini file.\nNOTE: Some settings (window size/position) require a restart to take effect.")
        {
            action = ImportConfig
        });

        items.Add(new CItemBase("Export Config", CItemBase.EPanelType.Normal,
            "現在の設定をiniファイルに書き出します。",
            "Export current settings to an external .ini file.")
        {
            action = ExportConfig
        });
        
        CItemList language = EnumChoice("Language", "表示言語を選択します。",
            "Display language.",
            () => CDTXMania.ConfigIni.languageMode, v => CDTXMania.ConfigIni.languageMode = v);
        CConfigIni.LanguageMode[] languageValues = Enum.GetValues<CConfigIni.LanguageMode>();
        language.action = () =>
        {
            // apply the newly-selected language immediately, then rebuild the page so its
            // descriptions re-render in that language (keeping the cursor on this row)
            CConfigIni.LanguageMode mode = languageValues[language.nCurrentlySelectedIndex];
            CDTXMania.ConfigIni.languageMode = mode;
            CDTXMania.ApplyLanguageMode(mode);

            List<CItemBase> rebuilt = Build();
            int index = rebuilt.FindIndex(i => i.strItemName == "Language");
            list.SetItems(rebuilt, index < 0 ? 0 : index);
        };
        items.Add(language);

        return items;
    }

    //todo: use nfd
    private void ImportConfig()
    {
        using OpenFileDialog dialog = new()
        {
            InitialDirectory = ".\\",
            FileName = "config.ini",
            Filter = "ini files (*.ini)|*.ini",
            FilterIndex = 2,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            Trace.TraceInformation("Cancel import of config");
            return;
        }

        try
        {
            CDTXMania.ConfigIni = new CConfigIni(dialog.FileName);
            Trace.TraceInformation("Imported config from " + dialog.FileName);

            //rebuild the page so the displayed values reflect the imported config
            list.SetItems(Build());
        }
        catch (Exception e)
        {
            Trace.TraceError("Failed to import config file: " + e.Message);
        }
    }

    private void ExportConfig()
    {
        using SaveFileDialog dialog = new()
        {
            InitialDirectory = ".\\",
            FileName = "config.ini",
            Filter = "ini files (*.ini)|*.ini",
            FilterIndex = 2,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            Trace.TraceInformation("Cancel export of config");
            return;
        }

        //values are written to ConfigIni live as they change, so it is already in sync
        CDTXMania.ConfigIni.tWrite(dialog.FileName);
        Trace.TraceInformation("Exported config to " + dialog.FileName);
    }
}
