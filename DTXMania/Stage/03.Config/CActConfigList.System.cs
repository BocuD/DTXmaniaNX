using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal partial class CActConfigList
{ 
    #region [ SKIN ]
        
    private CItemList iSystemSkinSubfolder;
        
    private CTexture? txSkinSample;				// #28195 2012.5.2 yyagi
    
    private void tGenerateSkinSample()
    {
        nSkinIndex = iSystemSkinSubfolder.nCurrentlySelectedIndex;
        if (nSkinSampleIndex != nSkinIndex)
        {
            string path = skinSubFolders[nSkinIndex];
            path = Path.Combine(path, @"Graphics\2_background.jpg");
            Bitmap bmSrc = new(path);
            Bitmap bmDest = new(GameFramebufferSize.Width, GameFramebufferSize.Height);
            Graphics g = Graphics.FromImage(bmDest);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmSrc,
                new Rectangle(60, 106, (int)(GameFramebufferSize.Width * 0.1984),
                    (int)(GameFramebufferSize.Height * 0.1984)),
                0, 0, 1280, 720, GraphicsUnit.Pixel);
            if (txSkinSample != null)
            {
                CDTXMania.tReleaseTexture(ref txSkinSample);
            }

            txSkinSample = CDTXMania.tGenerateTexture(bmDest, false);
            g.Dispose();
            bmDest.Dispose();
            bmSrc.Dispose();
            nSkinSampleIndex = nSkinIndex;
        }
    }
    
    private string[] skinSubFolders;			//
    private string[] skinNames;					//
    private string skinSubFolder_org;			//
    private int nSkinSampleIndex;				//
    private int nSkinIndex;						//
        
    private void ScanSkinFolders()
    {
        int ns = (CDTXMania.Skin.strSystemSkinSubfolders == null) ? 0 : CDTXMania.Skin.strSystemSkinSubfolders.Length;
        int nb = (CDTXMania.Skin.strBoxDefSkinSubfolders == null) ? 0 : CDTXMania.Skin.strBoxDefSkinSubfolders.Length;
            
        skinSubFolders = new string[ns + nb];
        for (int i = 0; i < ns; i++)
        {
            skinSubFolders[i] = CDTXMania.Skin.strSystemSkinSubfolders[i];
        }
        for (int i = 0; i < nb; i++)
        {
            skinSubFolders[ns + i] = CDTXMania.Skin.strBoxDefSkinSubfolders[i];
        }
        skinSubFolder_org = CDTXMania.Skin.GetCurrentSkinSubfolderFullName(true);
        Array.Sort(skinSubFolders);
        skinNames = CSkin.GetSkinName(skinSubFolders);
        nSkinIndex = Array.BinarySearch(skinSubFolders, skinSubFolder_org);
        if (nSkinIndex < 0)	// 念のため
        {
            nSkinIndex = 0;
        }
        nSkinSampleIndex = -1;
    }
        
    #endregion
    
    #region [ NEW SKIN ]
    private CItemList iNewSkinSelector;
    private string[] newSkinNames;
    private int nNewSkinIndex;

    private void ScanNewSkinData()
    {
        CDTXMania.SkinManager.ScanSkinDirectory();
        newSkinNames = CDTXMania.SkinManager.skins.Select(x => x.name).Prepend("None").ToArray();
        
        //find current skin index
        for (int i = 0; i < CDTXMania.SkinManager.skins.Count; i++)
        {
            var skin = CDTXMania.SkinManager.skins[i];
            if (skin.name == CDTXMania.SkinManager.currentSkin?.name)
            {
                nNewSkinIndex = i + 1; //account for none
                break;
            }
        }
    }
    
    private void ApplySkinChanges()
    {
        //Apply skin changes
        if (iNewSkinSelector.nCurrentlySelectedIndex != 0) //0 is none
        {
            CDTXMania.SkinManager.ChangeSkin(CDTXMania.SkinManager.skins[iNewSkinSelector.nCurrentlySelectedIndex - 1]); //account for none
        }
        else
        {
            CDTXMania.SkinManager.ChangeSkin(null);
        }
    }
    #endregion
    
    public void tSetupItemList_System()
    {
        tRecordToConfigIni();
        listItems.Clear();

        tAddReturnToMenuItem();
        
        CItemBase iSystemGoToGraphics = new("Graphics Options", CItemBase.EPanelType.Folder,
            "システムのグラフィック設定に関する項目を設定します。",
            "Open the graphics settings sub menu.")
        {
            action = tSetupItemList_Graphics
        };
        listItems.Add(iSystemGoToGraphics);
        
        CItemBase iSystemGoToAudio = new("Audio Options", CItemBase.EPanelType.Folder,
            "システムのオーディオ設定に関する項目を設定します。",
            "Open the audio settings sub menu.")
        {
            action = tSetupItemList_Audio
        };
        listItems.Add(iSystemGoToAudio);
        
        CItemBase iSystemGoToGameplay = new("Gameplay Options", CItemBase.EPanelType.Folder,
            "ゲーム設定に関する項目を設定します。",
            "Open the gameplay settings sub menu.")
        {
            action = tSetupItemList_Gameplay
        };
        listItems.Add(iSystemGoToGameplay);
        
        CItemBase iSystemGoToMenu = new("Menu Options", CItemBase.EPanelType.Folder,
            "メニュー設定に関する項目を設定します。",
            "Open the menu settings sub menu.")
        {
            action = tSetupItemList_Menu
        };
        listItems.Add(iSystemGoToMenu);

        CItemBase iSystemReloadDTX = new("Reload Songs", CItemBase.EPanelType.Normal,
            "曲データの一覧情報を\n"+
            "取得し直します。",
            "Clear song list cache and fully reload song data from disk.")
        {
            action = () =>
            {
                if (CDTXMania.SongDb.status == SongDbScanStatus.Idle)
                {
                    CDTXMania.SongDb.StartScan(() =>
                    {
                        CDTXMania.StageManager.stageSongSelectionNew.Reload();
                    });
                }
            }
        };
        listItems.Add(iSystemReloadDTX);
        
        int nDGmode = CDTXMania.ConfigIni.bDrumsEnabled ? 0 : 1;
        iSystemGRmode = new CItemList("Drums & GR ", CItemBase.EPanelType.Normal, nDGmode,
            "使用楽器の選択：\nDrOnly: ドラムのみ有効にします。\nGROnly: ギター/ベースのみの専用画面を\n用います。",
            "Instrument selection:\nDrOnly: Activate Drums screen.\nGROnly: Activate single screen for Guitar and Bass.\n",
            ["DrOnly", "GROnly"]);
        iSystemGRmode.BindConfig(
            () => iSystemGRmode.nCurrentlySelectedIndex = nDGmode, 
            () => { } );
        listItems.Add(iSystemGRmode);
            
        iSystemSkinSubfolder = new CItemList("Skin (Legacy)", CItemBase.EPanelType.Normal, nSkinIndex,
            "スキン切替：スキンを切り替えます。\n" +
            "\n",
            "Choose skin",
            skinNames);
        iSystemSkinSubfolder.BindConfig(() =>
            {
                //Handle updating of CDTXMania.ConfigIni.strSystemSkinSubfolderFullName back to UI value
                int nSkinIndex = -1;
                for (int i = 0; i < skinSubFolders.Length; i++)
                {
                    if (skinSubFolders[i] == CDTXMania.ConfigIni.strSystemSkinSubfolderFullName) {
                        nSkinIndex = i;
                        break;
                    }
                }
                
                if (nSkinIndex != -1) {

                    iSystemSkinSubfolder.nCurrentlySelectedIndex = nSkinIndex;
                    this.nSkinIndex = nSkinIndex;
                    CDTXMania.Skin.SetCurrentSkinSubfolderFullName(CDTXMania.ConfigIni.strSystemSkinSubfolderFullName, true);
                }
            },
            () => { });
        iSystemSkinSubfolder.action = tGenerateSkinSample;
        listItems.Add(iSystemSkinSubfolder);
        
        iNewSkinSelector = new CItemList("Skin (New)", CItemBase.EPanelType.Normal, nNewSkinIndex,
            "スキン切替：スキンを切り替えます。\n" +
            "\n",
            "Choose skin",
            newSkinNames);
        listItems.Add(iNewSkinSelector);

        CItemToggle iSystemUseBoxDefSkin = new("Skin (Box)", CDTXMania.ConfigIni.bUseBoxDefSkin,
            "Music boxスキンの利用：\n" +
            "特別なスキンが設定されたMusic box\n" +
            "に出入りしたときに、自動でスキンを\n" +
            "切り替えるかどうかを設定します。\n",
            "Box skin:\n" +
            "Automatically change skin as per box.def file.");
        iSystemUseBoxDefSkin.BindConfig(
            () => iSystemUseBoxDefSkin.bON = CDTXMania.ConfigIni.bUseBoxDefSkin,
            () => CDTXMania.ConfigIni.bUseBoxDefSkin = iSystemUseBoxDefSkin.bON);
        iSystemUseBoxDefSkin.action = () => CSkin.bUseBoxDefSkin = iSystemUseBoxDefSkin.bON;
        listItems.Add(iSystemUseBoxDefSkin);

        CItemList iInfoType = new("InfoType", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nInfoType,
            "Helpボタンを押した時に出る\n" +
            "情報表示を変更できます。\n" +
            "Type-A FPS、BGMアジャスト\n" +
            "などの情報が出ます。\n" +
            "Type-B 判定数などが出ます。\n",
            "Type-A: FPS, BGM adjustment are display\n" +
            "Type-B: Number of perfect/great etc. skill rate are displayed.",
            ["Type-A", "Type-B"]);
        iInfoType.BindConfig(
            () => iInfoType.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nInfoType,
            () => CDTXMania.ConfigIni.nInfoType = iInfoType.nCurrentlySelectedIndex);
        listItems.Add(iInfoType);
        
        CItemToggle iSystemBufferedInput = new("BufferedInput", CDTXMania.ConfigIni.bBufferedInput,
            "バッファ入力モード：\nON にすると、FPS を超える入力解像\n度を実現します。\nOFF にすると、入力解像度は FPS に\n等しくなります。",
            "Select joystick/keyboard/\nmouse input buffer mode.\nON to use buffer input. No lost/lags.\n"+
            "OFF to use realtime input. May cause lost/lags for input. Input frequency is synchronized with FPS.");
        iSystemBufferedInput.BindConfig(
            () => iSystemBufferedInput.bON = CDTXMania.ConfigIni.bBufferedInput,
            () => CDTXMania.ConfigIni.bBufferedInput = iSystemBufferedInput.bON);
        listItems.Add(iSystemBufferedInput);
        
        CItemToggle iSystemDebugInfo = new("Debug Info", CDTXMania.ConfigIni.bShowPerformanceInformation,
            "演奏情報の表示：\n" +
            "演奏中、BGA領域の下部に\n" +
            "演奏情報を表示します。\n" +
            "また、小節線の横に\n"+
            "小節番号が表示されるように\n"+
            "なります。",
            "Show song information on playing BGA area (FPS, BPM, total time etc)\nYou can turn ON/OFF the indications by pushing [Del] while playing drums, guitar or bass.");
        iSystemDebugInfo.BindConfig(
            () => iSystemDebugInfo.bON = CDTXMania.ConfigIni.bShowPerformanceInformation,
            () => CDTXMania.ConfigIni.bShowPerformanceInformation = iSystemDebugInfo.bON);
        listItems.Add(iSystemDebugInfo);
        
        CItemToggle iLogOutputLog = new("TraceLog", CDTXMania.ConfigIni.bOutputLogs,
            "Traceログ出力：\nDTXManiaLog.txt にログを出力します。\n変更した場合は、DTXMania の再起動\n後に有効となります。",
            "Turn ON to output debug logs to DTXManiaLog.txt file\nEffective after next DTXMania restart.");
        iLogOutputLog.BindConfig(
            () => iLogOutputLog.bON = CDTXMania.ConfigIni.bOutputLogs,
            () => CDTXMania.ConfigIni.bOutputLogs = iLogOutputLog.bON);
        listItems.Add(iLogOutputLog);

        CItemList iSystemChipPlayTimeComputeMode = new("Chip Timing Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            "発声時刻の計算方式を選択\n" +
            "します。\n" +
            "Original: 原発声時刻の計算方式\n" +
            "Accurate: BPM変更の時刻偏差修正",
            "Select Chip Timing Mode:\n" +
            "Original: Compatible with other DTXMania players\n" +
            "Accurate: Fixes time loss issue of BPM/Bar-Length Changes\n" +
            "NOTE: Only songs with many BPM/Bar-Length changes have observable time differences. Most songs are not affected by this option.",
            ["Original", "Accurate"]);
        iSystemChipPlayTimeComputeMode.BindConfig(
            () => iSystemChipPlayTimeComputeMode.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            () => CDTXMania.ConfigIni.nChipPlayTimeComputeMode = iSystemChipPlayTimeComputeMode.nCurrentlySelectedIndex);
        listItems.Add(iSystemChipPlayTimeComputeMode);
        
        CItemToggle iDiscordRichPresence = new("Discord Integration", CDTXMania.ConfigIni.bDiscordRichPresenceEnabled,
            "Discord Rich Presence：\n" +
            "Discordのステータスを更新します。\n" +
            "ONにすると、現在の曲名や\n" +
            "プレイ中のモードなどが表示されます。",
            "Enable Discord Rich Presence to update your Discord status with current song and playing mode.");
        iDiscordRichPresence.BindConfig(
            () => iDiscordRichPresence.bON = CDTXMania.ConfigIni.bDiscordRichPresenceEnabled,
            () => CDTXMania.ConfigIni.bDiscordRichPresenceEnabled = iDiscordRichPresence.bON);
        listItems.Add(iDiscordRichPresence);
        
        CItemBase iSystemGoToKeyAssign = new("System Key Mapping", CItemBase.EPanelType.Folder,
            "システムのキー入力に関する項目を設定します。",
            "System key input mapping configuration.")
        {
            action = tSetupItemList_KeyAssignSystem
        };
        listItems.Add(iSystemGoToKeyAssign);
        
        CItemBase iSystemImportConfig = new("Import Config", CItemBase.EPanelType.Normal,
            "config.iniファイルから設定\n" +
            "を再読み込みする。",
            "Import and apply settings from an external config.ini file.\nNOTE: Certain configurations such as Window Size and Position require restart of the application to take effect.")
        {
            action = () =>
            {
                //Import Config                 
                using OpenFileDialog openFileDialog = new();
                
                openFileDialog.InitialDirectory = ".\\";
                openFileDialog.FileName = "config.ini";
                openFileDialog.Filter = "ini files (*.ini)|*.ini";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    string filePath = openFileDialog.FileName;

                    Trace.TraceInformation("Selected File to import: " + filePath);
                    try
                    {
                        CConfigIni newConfig = new(filePath);
                        CDTXMania.ConfigIni = newConfig;
                        //Update the display values in config page to ensure UI is in-sync
                        tUpdateDisplayValuesFromConfigIni();
                        //Update Toast Message
                        string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                        tUpdateToastMessage($"Imported {fileName} successfully.");
                        ctToastMessageCounter.tStart(0, 1, 10000, CDTXMania.Timer);
                    }
                    catch (Exception)
                    {
                        Trace.TraceError("Fail to import config file");
                        tUpdateToastMessage("Error importing selected file.");
                        ctToastMessageCounter.tStart(0, 1, 10000, CDTXMania.Timer);
                    }
                }
                else
                {
                    Trace.TraceInformation("Cancel import of config");
                }
            }
        };
        listItems.Add(iSystemImportConfig);

        CItemBase iSystemExportConfig = new("Export Config", CItemBase.EPanelType.Normal,
            "config.iniファイルから設定\n" +
            "を再読み込みする。",
            "Export current settings to an external .ini file")
        {
            action = () =>
            {
                //Export Config                    
                using SaveFileDialog saveFileDialog = new();
                
                saveFileDialog.InitialDirectory = ".\\";
                saveFileDialog.FileName = "config.ini";
                saveFileDialog.Filter = "ini files (*.ini)|*.ini";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    string filePath = saveFileDialog.FileName;
                    Trace.TraceInformation("Selected File to export: " + filePath);
                    //Ensure changes are recorded to config.ini internally before export
                    tRecordToConfigIni();
                    CDTXMania.ConfigIni.tWrite(filePath); // CONFIGだけ
                    //Update Toast Message
                    string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                    tUpdateToastMessage($"Configurations exported to {fileName}.");
                    ctToastMessageCounter.tStart(0, 1, 10000, CDTXMania.Timer);
                }
                else
                {
                    Trace.TraceInformation("Cancel export of config");
                }
            }
        };
        listItems.Add(iSystemExportConfig);

        InitializeList();
        
        nCurrentSelection = 0;
        eMenuType = EMenuType.System;
    }

    private void tSetupItemList_KeyAssignSystem()
    {
        listItems.Clear();
            
        CItemBase iKeyAssignSystemReturnToMenu = new("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_System
        };
        listItems.Add(iKeyAssignSystemReturnToMenu);

        CItemBase iKeyAssignSystemCapture = new("Capture",
            "キャプチャキー設定：\n画面キャプチャのキーの割り当てを設\n定します。",
            "Capture key assign:\nTo assign key for screen capture.\n (You can use keyboard only. You can't\nuse pads to capture screenshot.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Capture)
        };
        listItems.Add(iKeyAssignSystemCapture);

        CItemBase iKeyAssignSystemSearch = new("Search",
            "サーチボタンのキー設定：\nサーチボタンへのキーの割り当\nてを設定します。",
            "Search button key assign:\nTo assign key for Search Button.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Search)
        };
        listItems.Add(iKeyAssignSystemSearch);

        CItemBase iKeyAssignGuitarHelp = new("Help",
            "ヘルプボタンのキー設定：\nヘルプボタンへのキーの割り当\nてを設定します。",
            "Help button key assign:\nTo assign key/pads for Help button.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Help)
        };
        listItems.Add(iKeyAssignGuitarHelp);

        CItemBase iKeyAssignBassHelp = new("Pause",
            "一時停止キー設定：\n 一時停止キーの割り当てを設定します。",
            "Pause key assign:\n To assign key/pads for Pause button.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.BASS, EKeyConfigPad.Help)
        };
        listItems.Add(iKeyAssignBassHelp);

        CItemBase iKeyAssignSystemLoopCreate = new("Loop Create",
            "",
            "Loop Create assign:\n To assign key/pads for loop creation.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopCreate)
        };
        listItems.Add(iKeyAssignSystemLoopCreate);

        CItemBase iKeyAssignSystemLoopDelete = new("Loop Delete",
            "",
            "Pause key assign:\n To assign key/pads for loop deletion.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopDelete)
        };
        listItems.Add(iKeyAssignSystemLoopDelete);

        CItemBase iKeyAssignSystemSkipForward = new("Skip forward",
            "",
            "Skip forward assign:\n To assign key/pads for Skip forward.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipForward)
        };
        listItems.Add(iKeyAssignSystemSkipForward);

        CItemBase iKeyAssignSystemSkipBackward = new("Skip backward",
            "",
            "Skip backward assign:\n To assign key/pads for Skip backward (rewind).")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipBackward)
        };
        listItems.Add(iKeyAssignSystemSkipBackward);

        CItemBase iKeyAssignSystemIncreasePlaySpeed = new("Increase play speed",
            "",
            "Increase play speed assign:\n To assign key/pads for increasing play speed.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.IncreasePlaySpeed)
        };
        listItems.Add(iKeyAssignSystemIncreasePlaySpeed);

        CItemBase iKeyAssignSystemDecreasePlaySpeed = new("Decrease play speed",
            "",
            "Decrease play speed assign:\n To assign key/pads for decreasing play speed.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.DecreasePlaySpeed)
        };
        listItems.Add(iKeyAssignSystemDecreasePlaySpeed);

        CItemBase iKeyAssignSystemRestart = new("Restart",
            "",
            "Restart assign:\n To assign key/pads for Restart button.")
        {
            action = () => stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Restart)
        };
        listItems.Add(iKeyAssignSystemRestart);
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.KeyAssignSystem;
    }
}
