﻿using System.Diagnostics;
using System.Windows.Forms;
using DTXMania.Core;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal partial class CActConfigList
{ 
    #region [ AUDIO ]
    private int iSystemSoundType_initial;
    private int iSystemWASAPIBufferSizeMs_initial;
    private int iSystemASIODevice_initial;
    private int iSystemSoundTimerType_initial;
    private CItemInteger iSystemMasterVolume;
    private CItemToggle iSystemTimeStretch;
        
    private CItemList iSystemSoundType;
    private CItemInteger iSystemWASAPIBufferSizeMs;
    private CItemList iSystemASIODevice;
    private CItemToggle iSystemSoundTimerType;
    private CItemToggle iSystemWASAPIEventDriven;
        
    private void CacheCurrentSoundDevices()
    {
        iSystemSoundType_initial = iSystemSoundType.n現在選択されている項目番号; // CONFIGに入ったときの値を保持しておく
        iSystemWASAPIBufferSizeMs_initial = iSystemWASAPIBufferSizeMs.nCurrentValue; // CONFIG脱出時にこの値から変更されているようなら
        iSystemASIODevice_initial = iSystemASIODevice.n現在選択されている項目番号; //
    }
    private void HandleSoundDeviceChanges()
    {
        if (iSystemSoundType_initial != iSystemSoundType.n現在選択されている項目番号 ||
            iSystemWASAPIBufferSizeMs_initial != iSystemWASAPIBufferSizeMs.nCurrentValue ||
            iSystemASIODevice_initial != iSystemASIODevice.n現在選択されている項目番号 ||
            iSystemSoundTimerType_initial != iSystemSoundTimerType.GetIndex())
        {
            ESoundDeviceType soundDeviceType;
            switch (iSystemSoundType.n現在選択されている項目番号)
            {
                case 0:
                    soundDeviceType = ESoundDeviceType.DirectSound;
                    break;
                case 1:
                    soundDeviceType = ESoundDeviceType.ASIO;
                    break;
                case 2:
                    soundDeviceType = ESoundDeviceType.ExclusiveWASAPI;
                    break;
                case 3:
                    soundDeviceType = ESoundDeviceType.SharedWASAPI;
                    break;
                default:
                    soundDeviceType = ESoundDeviceType.Unknown;
                    break;
            }

            CDTXMania.SoundManager.t初期化(soundDeviceType,
                iSystemWASAPIBufferSizeMs.nCurrentValue,
                false,
                0,
                iSystemASIODevice.n現在選択されている項目番号,
                iSystemSoundTimerType.bON);
            //CDTXMania.app.ShowWindowTitleWithSoundType();   //XGオプション
            CDTXMania.app.AddSoundTypeToWindowTitle();    //GDオプション
        }
    }
        
    #endregion
    #region [ SKIN ]
        
    private CItemList iSystemSkinSubfolder;
        
    private CTexture txSkinSample1;				// #28195 2012.5.2 yyagi
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
        
    public void tSetupItemList_System()
    {
        tRecordToConfigIni();
        listItems.Clear();

        iSystemReturnToMenu = new CItemBase("<< ReturnTo Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.");
        listItems.Add(iSystemReturnToMenu);

        CItemBase iSystemReloadDTX = new("Reload Songs", CItemBase.EPanelType.Normal,
            "曲データの一覧情報を\n"+
            "取得し直します。",
            "Clear song list cache and fully reload song data from disk.")
        {
            action = () =>
            {
                if (CDTXMania.EnumSongs.IsEnumerating)
                {
                    // Debug.WriteLine( "バックグラウンドでEnumeratingSongs中だったので、一旦中断します。" );
                    CDTXMania.EnumSongs.Abort();
                    CDTXMania.actEnumSongs.OnDeactivate();
                }

                CDTXMania.EnumSongs.StartEnumFromDisk(false);
                CDTXMania.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
                CDTXMania.actEnumSongs.bコマンドでの曲データ取得 = true;
                CDTXMania.actEnumSongs.OnActivate();
            }
        };
        listItems.Add(iSystemReloadDTX);

        CItemBase iSystemFastReloadDTX = new("Fast Reload", CItemBase.EPanelType.Normal,
            "曲データの一覧情報を\n" +
            "取得し直します。",
            "Detect changes in DTX Data folder from song list cache and load these changes only.\nWARNING: This feature is experimental and may corrupt the song list cache. Select Reload Songs if something goes wrong.")
        {
            action = () =>
            {
                if (CDTXMania.EnumSongs.IsEnumerating)
                {
                    // Debug.WriteLine( "バックグラウンドでEnumeratingSongs中だったので、一旦中断します。" );
                    CDTXMania.EnumSongs.Abort();
                    CDTXMania.actEnumSongs.OnDeactivate();
                }

                CDTXMania.EnumSongs.StartEnumFromDisk(true);
                CDTXMania.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
                CDTXMania.actEnumSongs.bコマンドでの曲データ取得 = true;
                CDTXMania.actEnumSongs.OnActivate();
            }
        };
        listItems.Add(iSystemFastReloadDTX);

        CItemBase iSystemImportConfig = new("Import Config", CItemBase.EPanelType.Normal,
            "config.iniファイルから設定\n" +
            "を再読み込みする。",
            "Import and apply settings from an external config.ini file.\nNOTE: Certain configurations such as Window Size and Position require restart of the application to take effect.")
        {
            action = () =>
            {
                //Import Config                 
                string fileContent = string.Empty;
                string filePath = string.Empty;

                using (OpenFileDialog openFileDialog = new())
                {
                    openFileDialog.InitialDirectory = ".\\";
                    openFileDialog.FileName = "config.ini";
                    openFileDialog.Filter = "ini files (*.ini)|*.ini";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        filePath = openFileDialog.FileName;

                        Trace.TraceInformation("Selected File to import: " + filePath);
                        try
                        {
                            CConfigIni newConfig = new(filePath);
                            CDTXMania.ConfigIni = newConfig;
                            //Update the display values in config page to ensure UI is in-sync
                            tUpdateDisplayValuesFromConfigIni();
                            //Update Toast Message
                            string fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                            tUpdateToastMessage(string.Format("Imported {0} successfully.", fileName));
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
                string fileContent = string.Empty;
                using (SaveFileDialog saveFileDialog = new())
                {
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
                        tUpdateToastMessage(string.Format("Configurations exported to {0}.", fileName));
                        ctToastMessageCounter.tStart(0, 1, 10000, CDTXMania.Timer);
                    }
                    else
                    {
                        Trace.TraceInformation("Cancel export of config");
                    }
                }
            }
        };
        listItems.Add(iSystemExportConfig);

        int nDGmode = (CDTXMania.ConfigIni.bGuitarEnabled ? 1 : 1) + (CDTXMania.ConfigIni.bDrumsEnabled ? 0 : 1) - 1;
        iSystemGRmode = new CItemList("Drums & GR ", CItemBase.EPanelType.Normal, nDGmode,
            "使用楽器の選択：\nDrOnly: ドラムのみ有効にします。\nGROnly: ギター/ベースのみの専用画面を\n用います。",
            "Instrument selection:\nDrOnly: Activate Drums screen.\nGROnly: Activate single screen for Guitar and Bass.\n",
            new string[] { "DrOnly", "GROnly" });
        iSystemGRmode.BindConfig(
            () => iSystemGRmode.n現在選択されている項目番号 = nDGmode, 
            () => { } );
        listItems.Add(iSystemGRmode);
            
        CItemInteger iSystemRisky = new("Risky", 0, 10, CDTXMania.ConfigIni.nRisky,
            "設定した回数分\n" +
            "ミスをすると、強制的に\n"+
            "STAGE FAILEDになります。",
            "Risky mode:\nNumber of mistakes (Poor/Miss) before getting STAGE FAILED.\n"+
            "Set 0 to disable Risky mode.");
        iSystemRisky.BindConfig(
            () => iSystemRisky.nCurrentValue = CDTXMania.ConfigIni.nRisky,
            () => CDTXMania.ConfigIni.nRisky = iSystemRisky.nCurrentValue);
        listItems.Add(iSystemRisky);

        CItemList iSystemMovieMode = new("Movie Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieMode,
            "Movie Mode:\n0 = 非表示\n1 = 全画面\n2 = ウインドウモード\n3 = 全画面&ウインドウ\n演奏中にF5キーで切り替え。",
            "Movie Mode:\n0 = Hide\n1 = Full screen\n2 = Window mode\n3 = Both Full screen and window\nUse F5 to switch during game.",
            new string[] { "Off", "Full Screen", "Window Mode", "Both" });
        iSystemMovieMode.BindConfig(
            () => iSystemMovieMode.n現在選択されている項目番号 = CDTXMania.ConfigIni.nMovieMode,
            () => CDTXMania.ConfigIni.nMovieMode = iSystemMovieMode.n現在選択されている項目番号);
        listItems.Add(iSystemMovieMode);

        CItemList iSystemMovieAlpha = new("LaneAlpha", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nMovieAlpha,
            "レーンの透明度を指定します。\n0% が完全不透明で、\n100% が完全透明となります。",
            "Degree of transparency for Movie.\n\n0%=No transparency,\n100%=Completely transparent",
            new string[] { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" });
        iSystemMovieAlpha.BindConfig(
            () => iSystemMovieAlpha.n現在選択されている項目番号 = CDTXMania.ConfigIni.nMovieAlpha,
            () => CDTXMania.ConfigIni.nMovieAlpha = iSystemMovieAlpha.n現在選択されている項目番号);
        listItems.Add(iSystemMovieAlpha);

        iCommonPlaySpeed = new CItemInteger("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, CDTXMania.ConfigIni.nPlaySpeed,
            "曲の演奏速度を、速くしたり\n"+
            "遅くしたりすることができます。\n"+
            "※一部のサウンドカードでは、\n"+
            "正しく再生できない可能性が\n"+
            "あります。）",
            "Change the song speed.\nFor example, you can play in half speed by setting PlaySpeed = 0.500 for practice.\nNote: It also changes the song's pitch.");
        iCommonPlaySpeed.BindConfig(
            () => iCommonPlaySpeed.nCurrentValue = CDTXMania.ConfigIni.nPlaySpeed,
            () => CDTXMania.ConfigIni.nPlaySpeed = iCommonPlaySpeed.nCurrentValue);
        listItems.Add(iCommonPlaySpeed);

        iSystemTimeStretch = new CItemToggle("TimeStretch", CDTXMania.ConfigIni.bTimeStretch,
            "演奏速度の変更方式:\n" +
            "ONにすると、\n"+
            "演奏速度の変更を、\n" +
            "周波数変更ではなく\n" +
            "タイムストレッチで行います。",
            "PlaySpeed mode:\n" +
            "Turn ON to use time stretch instead of frequency change.");
        iSystemTimeStretch.BindConfig(
            () => iSystemTimeStretch.bON = CDTXMania.ConfigIni.bTimeStretch,
            () => CDTXMania.ConfigIni.bTimeStretch = iSystemTimeStretch.bON);
        listItems.Add(iSystemTimeStretch);
            
        CItemList iSystemSkillMode = new("SkillMode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSkillMode,
            "達成率、スコアの計算方法を変更します。\n" +
            "CLASSIC:V6までのスコア計算とV8までの\n" +
            "ランク計算です。\n" +
            "XG:XGシリーズの達成率計算とV7以降の\n" +
            "スコア計算です。",
            "Skill rate and score calculation method\nCLASSIC: Pre-V6 score calculation and pre-V8 rank calculation\nXG: Current score and rank calculation",
            new string[] { "CLASSIC", "XG" });
        iSystemSkillMode.BindConfig(
            () => iSystemSkillMode.n現在選択されている項目番号 = CDTXMania.ConfigIni.nSkillMode,
            () => CDTXMania.ConfigIni.nSkillMode = iSystemSkillMode.n現在選択されている項目番号);
        listItems.Add(iSystemSkillMode);

        CItemToggle iSystemClassicNotes = new("CLASSIC Notes", CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする,
            "CLASSIC譜面の判別の有無を設定します。\n",
            "Use CLASSIC score calculation when a classic song is detected.\n");
        iSystemClassicNotes.BindConfig(
            () => iSystemClassicNotes.bON = CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする,
            () => CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする = iSystemClassicNotes.bON);
        listItems.Add(iSystemClassicNotes);

        CItemToggle iSystemFullscreen = new("Fullscreen", CDTXMania.ConfigIni.bFullScreenMode,
            "画面モード設定：\n ON で全画面モード、\n OFF でウィンドウモードになります。",
            "Fullscreen mode or window mode.")
        {
            action = () => CDTXMania.app.changeFullscreenModeOnNextFrame = true
        };
        iSystemFullscreen.BindConfig(
            () =>
            {
                if (iSystemFullscreen.bON != CDTXMania.ConfigIni.bFullScreenMode)
                {
                    //NOTE: The assignment is done in reverse because ConfigIni.bFullScreenMode will be toggled by the Draw method once the update flag is set to true
                    CDTXMania.ConfigIni.bFullScreenMode = iSystemFullscreen.bON;
                    CDTXMania.app.changeFullscreenModeOnNextFrame = true;
                    //Since actual value has changed, the UI should also reflect this
                    iSystemFullscreen.bON = !iSystemFullscreen.bON;
                }
            },
            () => iSystemFullscreen.bON = CDTXMania.ConfigIni.bFullScreenMode);
            
        listItems.Add(iSystemFullscreen);
            
        CItemToggle iSystemStageFailed = new("StageFailed", CDTXMania.ConfigIni.bSTAGEFAILEDEnabled,
            "ONにするとゲージが\n" +
            "なくなった時にSTAGE FAILED" +
            "となり演奏が中断されます。",
            "Turn OFF if you don't want to encounter STAGE FAILED.");
        iSystemStageFailed.BindConfig(
            () => iSystemStageFailed.bON = CDTXMania.ConfigIni.bSTAGEFAILEDEnabled,
            () => CDTXMania.ConfigIni.bSTAGEFAILEDEnabled = iSystemStageFailed.bON);
        listItems.Add(iSystemStageFailed);

        CItemToggle iSystemRandomFromSubBox = new("RandSubBox", CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            "子BOXをRANDOMの対象とする：\nON にすると、RANDOM SELECT 時に、\n子BOXも選択対象とします。",
            "Turn ON to use child BOX (subfolders) at RANDOM SELECT.");
        iSystemRandomFromSubBox.BindConfig(
            () => iSystemRandomFromSubBox.bON = CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            () => CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする = iSystemRandomFromSubBox.bON);
        listItems.Add(iSystemRandomFromSubBox);
            
        CItemToggle iSystemAdjustWaves = new("AdjustWaves", CDTXMania.ConfigIni.bWave再生位置自動調整機能有効,
            "サウンド再生位置自動補正：\n" +
            "ハードウェアやOSに起因する\n" +
            "サウンドのずれを補正します。\n" +
            "再生時間の長い曲で\n"+
            "効果があります。\n" +
            "※DirectSound使用時のみ有効です。",
            "Automatic wave playing position adjustment feature. When turned on, decreases the lag coming from the difference of hardware/OS.\n" +
            "Usually, you should turn it ON.\n"+
            "Note: This setting is effective only when DirectSound is used.");
        iSystemAdjustWaves.BindConfig(
            () => iSystemAdjustWaves.bON = CDTXMania.ConfigIni.bWave再生位置自動調整機能有効,
            () => CDTXMania.ConfigIni.bWave再生位置自動調整機能有効 = iSystemAdjustWaves.bON);
        listItems.Add(iSystemAdjustWaves);

        CItemToggle iSystemVSyncWait = new("VSyncWait", CDTXMania.ConfigIni.bVerticalSyncWait,
            "垂直帰線同期：\n" +
            "画面の描画をディスプレイの\n" +
            "垂直帰線中に行なう場合には\n" +
            "ONを指定します。\n" + 
            "ONにすると、ガタつきのない\n" +
            "滑らかな画面描画が実現されます。",
            "Turn ON to wait VSync (Vertical Synchronizing signal) at every drawing (so FPS becomes 60)\nIf you have enough CPU/GPU power, the scrolling would become smooth.");
        iSystemVSyncWait.action = () =>
        {
            CDTXMania.ConfigIni.bVerticalSyncWait = iSystemVSyncWait.bON;
            CDTXMania.app.changeVSyncModeOnNextFrame = true;
        };
        iSystemVSyncWait.BindConfig(
            () =>
            {
                if (iSystemVSyncWait.bON != CDTXMania.ConfigIni.bVerticalSyncWait) {
                    CDTXMania.app.changeVSyncModeOnNextFrame = true;
                }            
                iSystemVSyncWait.bON = CDTXMania.ConfigIni.bVerticalSyncWait;
            }, 
            () => CDTXMania.ConfigIni.bVerticalSyncWait = iSystemVSyncWait.bON);
        listItems.Add(iSystemVSyncWait);

        CItemToggle iSystemAVI = new("AVI", CDTXMania.ConfigIni.bAVIEnabled,
            "AVIの使用：\n動画(AVI)を再生可能にする場合に\nON にします。AVI の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable video (AVI) playback.\nThis requires some processing power.");
        iSystemAVI.BindConfig(
            () => iSystemAVI.bON = CDTXMania.ConfigIni.bAVIEnabled,
            () => CDTXMania.ConfigIni.bAVIEnabled = iSystemAVI.bON);
        listItems.Add(iSystemAVI);

        CItemToggle iSystemBGA = new("BGA", CDTXMania.ConfigIni.bBGAEnabled,
            "BGAの使用：\n画像(BGA)を表示可能にする場合に\nON にします。BGA の再生には、それ\nなりのマシンパワーが必要とされます。",
            "Turn ON to enable background animation (BGA) playback.\nThis requires some processing power.");
        iSystemBGA.BindConfig(
            () => iSystemBGA.bON = CDTXMania.ConfigIni.bBGAEnabled,
            () => CDTXMania.ConfigIni.bBGAEnabled = iSystemBGA.bON);
        listItems.Add(iSystemBGA);
            
        CItemInteger iSystemPreviewSoundWait = new("PreSoundWait", 0, 0x2710, CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms,
            "カーソルが合わされてから\n"+
            "プレビュー音が鳴り始める\n"+
            "までの時間を指定します。\n"+
            "0～10000[ms]が指定可能です。",
            "Delay time (ms) to start playing preview sound in song selection screen.\nYou can specify from 0ms to 10000ms.");
        iSystemPreviewSoundWait.BindConfig(
            () => iSystemPreviewSoundWait.nCurrentValue = CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms,
            () => CDTXMania.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms = iSystemPreviewSoundWait.nCurrentValue);
        listItems.Add(iSystemPreviewSoundWait);

        CItemInteger iSystemPreviewImageWait = new("PreImageWait", 0, 0x2710, CDTXMania.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms,
            "カーソルが合わされてから\n"+
            "プレビュー画像が表示\n"+
            "されるまでの時間を\n"+
            "指定します。\n"+
            "0～10000[ms]が指定可能です。",
            "Delay time (ms) to show preview image in song selection screen.\nYou can specify from 0ms to 10000ms.");
        iSystemPreviewImageWait.BindConfig(
            () => iSystemPreviewImageWait.nCurrentValue = CDTXMania.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms,
            () => CDTXMania.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms = iSystemPreviewImageWait.nCurrentValue);
        listItems.Add(iSystemPreviewImageWait);
            
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
            
        CItemInteger iSystemBGAlpha = new("BG Alpha", 0, 0xff, CDTXMania.ConfigIni.nBackgroundTransparency,
            "背景画像をDTXManiaの\n"+
            "背景画像と合成する際の\n"+
            "背景画像の透明度を\n"+
            "指定します。\n"+
            "255に近いほど、不透明\n"+
            "になります。",
            "Degree of transparency for background wallpaper\n\n0=Completely transparent,\n255=No transparency");
        iSystemBGAlpha.BindConfig(
            () => iSystemBGAlpha.nCurrentValue = CDTXMania.ConfigIni.nBackgroundTransparency,
            () => CDTXMania.ConfigIni.nBackgroundTransparency = iSystemBGAlpha.nCurrentValue);
        listItems.Add(iSystemBGAlpha);

        CItemToggle iSystemBGMSound = new("BGM Sound", CDTXMania.ConfigIni.bBGM音を発声する,
            "OFFにするとBGMを再生しません。",
            "Turn OFF if you don't want to play the song music (BGM).");
        iSystemBGMSound.BindConfig(
            () => iSystemBGMSound.bON = CDTXMania.ConfigIni.bBGM音を発声する,
            () => CDTXMania.ConfigIni.bBGM音を発声する = iSystemBGMSound.bON);
        listItems.Add(iSystemBGMSound);
            
        CItemToggle iSystemAudienceSound = new("Audience", CDTXMania.ConfigIni.b歓声を発声する,
            "OFFにすると歓声を再生しません。",
            "Turn ON if you want to be cheered at the end of fill-in zone or not.");
        iSystemAudienceSound.BindConfig(
            () => iSystemAudienceSound.bON = CDTXMania.ConfigIni.b歓声を発声する,
            () => CDTXMania.ConfigIni.b歓声を発声する = iSystemAudienceSound.bON);
        listItems.Add(iSystemAudienceSound);

        CItemList iSystemDamageLevel = new("DamageLevel", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDamageLevel,
            "Miss時のゲージの減少度合い\n"+
            "を指定します。\n"+
            "Risky時は無効となります",
            "Degree of decrease of the damage gauge when missing chips.\nThis setting is ignored when Risky >= 1.",
            new string[] { "Small", "Normal", "Large" });
        iSystemDamageLevel.BindConfig(
            () => iSystemDamageLevel.n現在選択されている項目番号 = (int)CDTXMania.ConfigIni.eDamageLevel,
            () => CDTXMania.ConfigIni.eDamageLevel = (EDamageLevel)iSystemDamageLevel.n現在選択されている項目番号);
        listItems.Add(iSystemDamageLevel);

        CItemToggle iSystemSaveScore = new("SaveScore", CDTXMania.ConfigIni.bScoreIniを出力する,
            "演奏記録の保存：\n"+
            "ONで演奏記録を.score.iniに\n"+
            "保存します。\n",
            "Turn ON to save high scores/skills.\nTurn OFF in case your song data are on read-only media.\n"+
            "Note that the score files also contain 'BGM Adjust' parameter, so turn ON to keep adjustment.");
        iSystemSaveScore.BindConfig(
            () => iSystemSaveScore.bON = CDTXMania.ConfigIni.bScoreIniを出力する,
            () => CDTXMania.ConfigIni.bScoreIniを出力する = iSystemSaveScore.bON);
        listItems.Add(iSystemSaveScore);
            
        CItemInteger iSystemChipVolume = new("ChipVolume", 0, 100, CDTXMania.ConfigIni.n手動再生音量,
            "打音の音量：\n入力に反応して再生される\nチップの音量を指定します。\n0 ～ 100 % の値が指定可能\nです。\n",
            "Volume for chips you hit.\nYou can specify from 0 to 100%.");
        iSystemChipVolume.BindConfig(
            () => iSystemChipVolume.nCurrentValue = CDTXMania.ConfigIni.n手動再生音量,
            () => CDTXMania.ConfigIni.n手動再生音量 = iSystemChipVolume.nCurrentValue);
        listItems.Add(iSystemChipVolume);

        CItemInteger iSystemAutoChipVolume = new("AutoVolume", 0, 100, CDTXMania.ConfigIni.n自動再生音量,
            "自動再生音の音量：\n自動的に再生される\nチップの音量を指定します。\n0 ～ 100 % の値が指定可能\nです。\n",
            "Volume for AUTO chips.\nYou can specify from 0 to 100%.");
        iSystemAutoChipVolume.BindConfig(
            () => iSystemAutoChipVolume.nCurrentValue = CDTXMania.ConfigIni.n自動再生音量,
            () => CDTXMania.ConfigIni.n自動再生音量 = iSystemAutoChipVolume.nCurrentValue);
        listItems.Add(iSystemAutoChipVolume);

        /*
        var iSystemStoicMode = new CItemToggle("StoicMode", CDTXMania.ConfigIni.bストイックモード,
            "ストイック（禁欲）モード：\n" +
            "以下をまとめて表示ON/OFFします。\n" +
            "_プレビュー画像/動画\n" +
            "_リザルト画像/動画\n" +
            "_NowLoading画像\n" +
            "_演奏画面の背景画像\n" +
            "_BGA 画像 / AVI 動画\n" +
            "_グラフ画像\n",
            "Turn ON to disable drawing\n * preview image / movie\n * result image / movie\n * nowloading image\n * wallpaper (in playing screen)\n * BGA / AVI (in playing screen)");
        this.listItems.Add(this.iSystemStoicMode);
        */
            
        CItemToggle iSystemStageEffect = new("StageEffect", CDTXMania.ConfigIni.DisplayBonusEffects,
            "OFFにすると、\n" +
            "ゲーム中の背景演出が\n" +
            "非表示になります。",
            "When turned off, background stage effects are disabled");
        iSystemStageEffect.BindConfig(
            () => iSystemStageEffect.bON = CDTXMania.ConfigIni.DisplayBonusEffects,
            () => CDTXMania.ConfigIni.DisplayBonusEffects = iSystemStageEffect.bON);
        listItems.Add(iSystemStageEffect);
            
        CItemList iSystemShowLag = new("ShowLagTime", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nShowLagType,
            "ズレ時間表示：\n"+
            "ジャストタイミングからの\n"+
            "ズレ時間(ms)を表示します。\n"+
            "OFF: 表示しません。\n"+
            "ON: ズレ時間を表示します。\n"+
            "GREAT-: PERFECT以外の時\n"+
            "のみ表示します。",
            "Display the lag from ideal hit time (ms)\nOFF: Don't show.\nON: Show.\nGREAT-: Show except for perfect chips.",
            new string[] { "OFF", "ON", "GREAT-" });
        iSystemShowLag.BindConfig(
            () => iSystemShowLag.n現在選択されている項目番号 = CDTXMania.ConfigIni.nShowLagType,
            () => CDTXMania.ConfigIni.nShowLagType = iSystemShowLag.n現在選択されている項目番号);
        listItems.Add(iSystemShowLag);

        CItemList iSystemShowLagColor = new("ShowLagTimeColor", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nShowLagTypeColor,
            "ズレ時間表示の表示色変更：\n  TYPE-A: 早ズレを青、遅ズレを赤で表示します。\n  TYPE-B: 早ズレを赤、遅ズレを青で表示します。",
            "Change color of lag time display：\nTYPE-A: early notes in blue and late notes in red.\nTYPE-B: early notes in red and late notes in blue.",
            new string[] { "TYPE-A", "TYPE-B" } );
        iSystemShowLagColor.BindConfig(
            () => iSystemShowLagColor.n現在選択されている項目番号 = CDTXMania.ConfigIni.nShowLagTypeColor,
            () => CDTXMania.ConfigIni.nShowLagTypeColor = iSystemShowLagColor.n現在選択されている項目番号);
        listItems.Add(iSystemShowLagColor);

        CItemToggle iSystemShowLagHitCount = new("ShowLagHitCount", CDTXMania.ConfigIni.bShowLagHitCount,
            "ズレヒット数表示:\n演奏と結果画面に早ズレ、遅ズレヒット数で表示する場合はONにします", //fisyher: Constructed using DeepL, feedback welcomed to improved accuracy
            "ShowLagHitCount:\nTurn ON to display Early/Late Hit Counters in Performance and Result Screen.");
        iSystemShowLagHitCount.BindConfig(
            () => iSystemShowLagHitCount.bON = CDTXMania.ConfigIni.bShowLagHitCount,
            () => CDTXMania.ConfigIni.bShowLagHitCount = iSystemShowLagHitCount.bON);
        listItems.Add(iSystemShowLagHitCount);

        CItemToggle iSystemAutoResultCapture = new("AutoSaveResult", CDTXMania.ConfigIni.bIsAutoResultCapture,
            "ONにすると、NewRecord時に\n"+
            "自動でリザルト画像を\n"+
            "曲データと同じフォルダに\n"+
            "保存します。",
            "AutoSaveResult:\nTurn ON to save your result screen image automatically when you get hiscore/hiskill.");
        iSystemAutoResultCapture.BindConfig(
            () => iSystemAutoResultCapture.bON = CDTXMania.ConfigIni.bIsAutoResultCapture,
            () => CDTXMania.ConfigIni.bIsAutoResultCapture = iSystemAutoResultCapture.bON);
        listItems.Add(iSystemAutoResultCapture);

        CItemToggle iSystemMusicNameDispDef = new("MusicNameDispDEF", CDTXMania.ConfigIni.b曲名表示をdefのものにする,
            "表示される曲名をdefのものにします。\n" +
            "ただし選曲画面の表示は、\n" +
            "defファイルの曲名が\n"+
            "優先されます。",
            "Display the music title from SET.def file");
        iSystemMusicNameDispDef.BindConfig(
            () => iSystemMusicNameDispDef.bON = CDTXMania.ConfigIni.b曲名表示をdefのものにする,
            () => CDTXMania.ConfigIni.b曲名表示をdefのものにする = iSystemMusicNameDispDef.bON);
        listItems.Add(iSystemMusicNameDispDef);
            
        CItemToggle iAutoAddGage = new("AutoAddGage", CDTXMania.ConfigIni.bAutoAddGage,
            "ONの場合、AUTO判定も\n"+
            "ゲージに加算されます。\n",
            "If ON, will be added to the judgment also gauge AUTO.\n" +
            "");
        iAutoAddGage.BindConfig(
            () => iAutoAddGage.bON = CDTXMania.ConfigIni.bAutoAddGage,
            () => CDTXMania.ConfigIni.bAutoAddGage = iAutoAddGage.bON);
        listItems.Add(iAutoAddGage);
            
        CItemToggle iSystemBufferedInput = new("BufferedInput", CDTXMania.ConfigIni.bバッファ入力を行う,
            "バッファ入力モード：\nON にすると、FPS を超える入力解像\n度を実現します。\nOFF にすると、入力解像度は FPS に\n等しくなります。",
            "Select joystick/keyboard/\nmouse input buffer mode.\nON to use buffer input. No lost/lags.\n"+
            "OFF to use realtime input. May cause lost/lags for input. Input frequency is synchronized with FPS.");
        iSystemBufferedInput.BindConfig(
            () => iSystemBufferedInput.bON = CDTXMania.ConfigIni.bバッファ入力を行う,
            () => CDTXMania.ConfigIni.bバッファ入力を行う = iSystemBufferedInput.bON);
        listItems.Add(iSystemBufferedInput);

        CItemToggle iLogOutputLog = new("TraceLog", CDTXMania.ConfigIni.bOutputLogs,
            "Traceログ出力：\nDTXManiaLog.txt にログを出力します。\n変更した場合は、DTXMania の再起動\n後に有効となります。",
            "Turn ON to output debug logs to DTXManiaLog.txt file\nEffective after next DTXMania restart.");
        iLogOutputLog.BindConfig(
            () => iLogOutputLog.bON = CDTXMania.ConfigIni.bOutputLogs,
            () => CDTXMania.ConfigIni.bOutputLogs = iLogOutputLog.bON);
        listItems.Add(iLogOutputLog);
            
        // #24820 2013.1.3 yyagi
        iSystemSoundType = new CItemList("SoundType", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSoundDeviceType,
            "サウンド出力方式を選択\n"+
            "します。\n" +
            "WASAPIはVista以降、\n"+
            "ASIOは対応機器でのみ使用可能です。\n" +
            "WASAPIかASIOを使うと、\n"+
            "遅延を少なくできます。\n",
            "DSound: Direct Sound\n" +
            "WASAPI: from Windows Vista\n" +
            "ASIO: with ASIO compatible devices only\n" +
            "Use WASAPI or ASIO to decrease the sound lag.\n" +
            "Note: Exit CONFIG to make the setting take effect.",
            new string[] { "DSound", "ASIO", "WASAPIExclusive", "WASAPIShared" });
        iSystemSoundType.BindConfig(
            () => iSystemSoundType.n現在選択されている項目番号 = CDTXMania.ConfigIni.nSoundDeviceType,
            () => CDTXMania.ConfigIni.nSoundDeviceType = iSystemSoundType.n現在選択されている項目番号);
        listItems.Add(iSystemSoundType);

        // #24820 2013.1.15 yyagi
        iSystemWASAPIBufferSizeMs = new CItemInteger("WASAPIBufSize", 0, 99999, CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            "WASAPI時のバッファサイズ:\n" +
            "0～99999msを指定できます。\n" +
            "0を指定すると、OSがサイズを\n" +
            "自動設定します。\n" +
            "値を小さくするほどラグが減少\n" +
            "しますが、音割れや異常を\n" +
            "引き起こす場合があります。\n",
            "Sound buffer size for WASAPI, from 0 to 99999ms.\n" +
            "Set 0 to use default system buffer size.\n" +
            "Small value reduces lag but may cause sound troubles.\n" +
            "Note: Exit CONFIG to make the setting take effect.");
        iSystemWASAPIBufferSizeMs.BindConfig(
            () => iSystemWASAPIBufferSizeMs.nCurrentValue = CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
            () => CDTXMania.ConfigIni.nWASAPIBufferSizeMs = iSystemWASAPIBufferSizeMs.nCurrentValue);
        listItems.Add(iSystemWASAPIBufferSizeMs);

        // #24820 2013.1.17 yyagi
        string[] asiodevs = CEnumerateAllAsioDevices.GetAllASIODevices();
        iSystemASIODevice = new CItemList("ASIO device", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nASIODevice,
            "ASIOデバイス:\n" +
            "ASIO使用時の\n" +
            "サウンドデバイスを選択\n"+
            "します。\n",
            "ASIO Sound Device:\n" +
            "Select the sound device to use under ASIO mode.\n" +
            "\n" +
            "Note: Exit CONFIG to make the setting take effect.",
            asiodevs);
        iSystemASIODevice.BindConfig(
            () => iSystemASIODevice.n現在選択されている項目番号 = CDTXMania.ConfigIni.nASIODevice,
            () => CDTXMania.ConfigIni.nASIODevice = iSystemASIODevice.n現在選択されている項目番号);
        listItems.Add(iSystemASIODevice);
        // #24820 2013.1.3 yyagi

        /*
        var iSystemASIOBufferSizeMs = new CItemInteger("ASIOBuffSize", 0, 99999, CDTXMania.ConfigIni.nASIOBufferSizeMs,
            "ASIO使用時のバッファサイズ:\n" +
            "0～99999ms を指定可能です。\n" +
            "0を指定すると、サウンドデバイスに\n" +
            "指定されている設定値を使用します。\n" +
            "値を小さくするほど発音ラグが\n" +
            "減少しますが、音割れや異常動作を\n" +
            "引き起こす場合があります。\n"+
            "※ 設定はCONFIGURATION画面の\n" +
            "　終了時に有効になります。",
            "Sound buffer size for ASIO:\n" +
            "You can set from 0 to 99999ms.\n" +
            "Set 0 to use a default value already\n" +
            "specified to the sound device.\n" +
            "Smaller value makes smaller lag,\n" +
            "but it may cause sound troubles.\n" +
            "\n" +
            "Note: Exit CONFIGURATION to make\n" +
            " the setting take effect.");
        this.listItems.Add(this.iSystemASIOBufferSizeMs);
        */
        iSystemSoundTimerType = new CItemToggle("UseOSTimer", CDTXMania.ConfigIni.bUseOSTimer,
            "OSタイマーを使用するかどうか:\n" +
            "演奏タイマーとして、DTXMania独自のタイマーを使うか\n" +
            "OS標準のタイマーを使うかを選択します。\n" +
            "OS標準タイマーを使うとスクロールが滑らかに\n" +
            "なりますが、演奏で音ズレが発生することが\n" +
            "あります。\n" +
            "(そのためAdjustWavesの効果が適用されます。)\n" +
            "\n" +
            "この指定はWASAPI/ASIO使用時のみ有効です。\n",
            "Use OS Timer or not:\n" +
            "If this settings is ON, DTXMania uses OS Standard timer. It brings smooth scroll, but may cause some sound lag.\n" +
            "(so AdjustWaves is also avilable)\n" +
            "\n" +
            "If OFF, DTXMania uses its original timer and the effect is vice versa.\n" +
            "\n" +
            "This settings is avilable only when you use WASAPI/ASIO.\n"
        );
        iSystemSoundTimerType.BindConfig(
            () => iSystemSoundTimerType.bON = CDTXMania.ConfigIni.bUseOSTimer,
            () => CDTXMania.ConfigIni.bUseOSTimer = iSystemSoundTimerType.bON);
        listItems.Add(iSystemSoundTimerType);

        // #33700 2013.1.3 yyagi
        iSystemMasterVolume = new CItemInteger("MasterVolume", 0, 100, CDTXMania.ConfigIni.nMasterVolume,
            "マスターボリュームの設定:\n" +
            "全体の音量を設定します。\n" +
            "0が無音で、100が最大値です。\n" +
            "(WASAPI/ASIO時のみ有効です)",
            "Master Volume:\n" +
            "You can set 0 - 100.\n" +
            "\n" +
            "Note:\n" +
            "Only for WASAPI/ASIO mode.");
        iSystemMasterVolume.BindConfig(
            () => iSystemMasterVolume.nCurrentValue = CDTXMania.ConfigIni.nMasterVolume,
            () => CDTXMania.ConfigIni.nMasterVolume = iSystemMasterVolume.nCurrentValue);
        listItems.Add(iSystemMasterVolume);

        iSystemWASAPIEventDriven = new CItemToggle("WASAPIEventDriven", CDTXMania.ConfigIni.bEventDrivenWASAPI,
            "WASAPIをEvent Drivenモードで使用します。\n" +
            "これを使うと、サウンド出力の遅延をより小さくできますが、システム負荷は上昇します。",
            "Use WASAPI Event Driven mode.\n" +
            "It reduce sound output lag, but it also decreases system performance.");
        listItems.Add(iSystemWASAPIEventDriven);

        #region [ GDオプション ]
            
        CItemToggle iSystemDifficulty = new("Difficulty", CDTXMania.ConfigIni.b難易度表示をXG表示にする,
            "選曲画面での難易度表示方法を変更します。\nON でXG風3ケタ、\nOFF で従来の2ケタ表示になります。",
            "Change difficulty display mode on song selection screen.\n"+
            "ON for XG-style 3-digit display\nOFF for classic 2-digit display.");
        iSystemDifficulty.BindConfig(
            () => iSystemDifficulty.bON = CDTXMania.ConfigIni.b難易度表示をXG表示にする,
            () => CDTXMania.ConfigIni.b難易度表示をXG表示にする = iSystemDifficulty.bON);
        listItems.Add(iSystemDifficulty);
            
        CItemToggle iSystemShowScore = new("ShowScore", CDTXMania.ConfigIni.bShowScore,
            "演奏中のスコアの表示の有無を設定します。",
            "Display the score during the game.");
        iSystemShowScore.BindConfig(
            () => iSystemShowScore.bON = CDTXMania.ConfigIni.bShowScore,
            () => CDTXMania.ConfigIni.bShowScore = iSystemShowScore.bON);
        listItems.Add(iSystemShowScore);

        CItemToggle iSystemShowMusicInfo = new("ShowMusicInfo", CDTXMania.ConfigIni.bShowMusicInfo,
            "OFFにすると演奏中のジャケット、曲情報を\n表示しません。",
            "When turned OFF, the cover and song information being played are not displayed.");
        iSystemShowMusicInfo.BindConfig(
            () => iSystemShowMusicInfo.bON = CDTXMania.ConfigIni.bShowMusicInfo,
            () => CDTXMania.ConfigIni.bShowMusicInfo = iSystemShowMusicInfo.bON);
        listItems.Add(iSystemShowMusicInfo);
             
        #endregion
            
        iSystemSkinSubfolder = new CItemList("Skin (General)", CItemBase.EPanelType.Normal, nSkinIndex,
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

                    iSystemSkinSubfolder.n現在選択されている項目番号 = nSkinIndex;
                    this.nSkinIndex = nSkinIndex;
                    CDTXMania.Skin.SetCurrentSkinSubfolderFullName(CDTXMania.ConfigIni.strSystemSkinSubfolderFullName, true);
                }
            },
            () => { });
        iSystemSkinSubfolder.action = tGenerateSkinSample;
        listItems.Add(iSystemSkinSubfolder);

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
            new string[] { "Type-A", "Type-B" });
        iInfoType.BindConfig(
            () => iInfoType.n現在選択されている項目番号 = CDTXMania.ConfigIni.nInfoType,
            () => CDTXMania.ConfigIni.nInfoType = iInfoType.n現在選択されている項目番号);
        listItems.Add(iInfoType);

        // #36372 2016.06.19 kairera0467
        CItemInteger iSystemBGMAdjust = new( "BGMAdjust", -99, 99, CDTXMania.ConfigIni.nCommonBGMAdjustMs,
            "BGMの再生タイミングの微調整を行います。\n" +
            "-99 ～ 99ms まで指定可能です。\n" +
            "値を指定してください。\n",
            "Adjust the BGM play timing.\n" +
            "You can set from -99 to 0 ms.\n" );
        iSystemBGMAdjust.BindConfig(
            () => iSystemBGMAdjust.nCurrentValue = CDTXMania.ConfigIni.nCommonBGMAdjustMs,
            () => CDTXMania.ConfigIni.nCommonBGMAdjustMs = iSystemBGMAdjust.nCurrentValue);
        listItems.Add(iSystemBGMAdjust);

        CItemBase iSystemGoToKeyAssign = new("System Keys", CItemBase.EPanelType.Normal,
            "システムのキー入力に関する項目を設定します。",
            "Settings for the system key/pad inputs.")
        {
            action = tSetupItemList_KeyAssignSystem
        };
        listItems.Add(iSystemGoToKeyAssign);
            
        CItemToggle iSystemMetronome = new("Metronome", CDTXMania.ConfigIni.bMetronome,
            "メトロノームを有効にします。", "Enable Metronome.");
        iSystemMetronome.BindConfig(
            () => iSystemMetronome.bON = CDTXMania.ConfigIni.bMetronome,
            () => CDTXMania.ConfigIni.bMetronome = iSystemMetronome.bON);
        listItems.Add(iSystemMetronome);

        CItemList iSystemChipPlayTimeComputeMode = new("Chip Timing Mode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            "発声時刻の計算方式を選択\n" +
            "します。\n" +
            "Original: 原発声時刻の計算方式\n" +
            "Accurate: BPM変更の時刻偏差修正",
            "Select Chip Timing Mode:\n" +
            "Original: Compatible with other DTXMania players\n" +
            "Accurate: Fixes time loss issue of BPM/Bar-Length Changes\n" +
            "NOTE: Only songs with many BPM/Bar-Length changes have observable time differences. Most songs are not affected by this option.",
            new string[] { "Original", "Accurate" });
        iSystemChipPlayTimeComputeMode.BindConfig(
            () => iSystemChipPlayTimeComputeMode.n現在選択されている項目番号 = CDTXMania.ConfigIni.nChipPlayTimeComputeMode,
            () => CDTXMania.ConfigIni.nChipPlayTimeComputeMode = iSystemChipPlayTimeComputeMode.n現在選択されている項目番号);
        listItems.Add(iSystemChipPlayTimeComputeMode);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.System;
    }
        
    public void tSetupItemList_KeyAssignSystem()
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
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Capture)
        };
        listItems.Add(iKeyAssignSystemCapture);

        CItemBase iKeyAssignSystemSearch = new("Search",
            "サーチボタンのキー設定：\nサーチボタンへのキーの割り当\nてを設定します。",
            "Search button key assign:\nTo assign key for Search Button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Search)
        };
        listItems.Add(iKeyAssignSystemSearch);

        CItemBase iKeyAssignGuitarHelp = new("Help",
            "ヘルプボタンのキー設定：\nヘルプボタンへのキーの割り当\nてを設定します。",
            "Help button key assign:\nTo assign key/pads for Help button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.GUITAR, EKeyConfigPad.Help)
        };
        listItems.Add(iKeyAssignGuitarHelp);

        CItemBase iKeyAssignBassHelp = new("Pause",
            "一時停止キー設定：\n 一時停止キーの割り当てを設定します。",
            "Pause key assign:\n To assign key/pads for Pause button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.BASS, EKeyConfigPad.Help)
        };
        listItems.Add(iKeyAssignBassHelp);

        CItemBase iKeyAssignSystemLoopCreate = new("Loop Create",
            "",
            "Loop Create assign:\n To assign key/pads for loop creation.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopCreate)
        };
        listItems.Add(iKeyAssignSystemLoopCreate);

        CItemBase iKeyAssignSystemLoopDelete = new("Loop Delete",
            "",
            "Pause key assign:\n To assign key/pads for loop deletion.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.LoopDelete)
        };
        listItems.Add(iKeyAssignSystemLoopDelete);

        CItemBase iKeyAssignSystemSkipForward = new("Skip forward",
            "",
            "Skip forward assign:\n To assign key/pads for Skip forward.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipForward)
        };
        listItems.Add(iKeyAssignSystemSkipForward);

        CItemBase iKeyAssignSystemSkipBackward = new("Skip backward",
            "",
            "Skip backward assign:\n To assign key/pads for Skip backward (rewind).")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.SkipBackward)
        };
        listItems.Add(iKeyAssignSystemSkipBackward);

        CItemBase iKeyAssignSystemIncreasePlaySpeed = new("Increase play speed",
            "",
            "Increase play speed assign:\n To assign key/pads for increasing play speed.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.IncreasePlaySpeed)
        };
        listItems.Add(iKeyAssignSystemIncreasePlaySpeed);

        CItemBase iKeyAssignSystemDecreasePlaySpeed = new("Decrease play speed",
            "",
            "Decrease play speed assign:\n To assign key/pads for decreasing play speed.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.DecreasePlaySpeed)
        };
        listItems.Add(iKeyAssignSystemDecreasePlaySpeed);

        CItemBase iKeyAssignSystemRestart = new("Restart",
            "",
            "Restart assign:\n To assign key/pads for Restart button.")
        {
            action = () => CDTXMania.stageConfig.tNotifyPadSelection(EKeyConfigPart.SYSTEM, EKeyConfigPad.Restart)
        };
        listItems.Add(iKeyAssignSystemRestart);

        OnListMenuの初期化();
        nCurrentSelection = 0;
        eMenuType = EMenuType.KeyAssignSystem;
    }
}