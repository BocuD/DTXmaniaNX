using DTXMania.Core;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal partial class CActConfigList
{
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

    private bool audioMenuOpened = false;
        
    private void CacheCurrentSoundDevices()
    {
        iSystemSoundType_initial = iSystemSoundType.nCurrentlySelectedIndex; // CONFIGに入ったときの値を保持しておく
        iSystemWASAPIBufferSizeMs_initial = iSystemWASAPIBufferSizeMs.nCurrentValue; // CONFIG脱出時にこの値から変更されているようなら
        iSystemASIODevice_initial = iSystemASIODevice.nCurrentlySelectedIndex; //

        audioMenuOpened = true;
    }
    private void HandleSoundDeviceChanges()
    {
        if (!audioMenuOpened)
        {
            return;
        }

        //reset for next load
        audioMenuOpened = false;

        if (iSystemSoundType_initial != iSystemSoundType.nCurrentlySelectedIndex ||
            iSystemWASAPIBufferSizeMs_initial != iSystemWASAPIBufferSizeMs.nCurrentValue ||
            iSystemASIODevice_initial != iSystemASIODevice.nCurrentlySelectedIndex ||
            iSystemSoundTimerType_initial != iSystemSoundTimerType.GetIndex())
        {
            ESoundDeviceType soundDeviceType;
            switch (iSystemSoundType.nCurrentlySelectedIndex)
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
                iSystemASIODevice.nCurrentlySelectedIndex,
                iSystemSoundTimerType.bON);
            //CDTXMania.app.ShowWindowTitleWithSoundType();   //XGオプション
            CDTXMania.app.AddSoundTypeToWindowTitle();    //GDオプション
        }
    }
    
    private void tSetupItemList_Audio()
    {
        listItems.Clear();

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
            ["DSound", "ASIO", "WASAPIExclusive", "WASAPIShared"]);
        iSystemSoundType.BindConfig(
            () => iSystemSoundType.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nSoundDeviceType,
            () => CDTXMania.ConfigIni.nSoundDeviceType = iSystemSoundType.nCurrentlySelectedIndex);
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
        
        iSystemWASAPIEventDriven = new CItemToggle("WASAPIEventDriven", CDTXMania.ConfigIni.bEventDrivenWASAPI,
            "WASAPIをEvent Drivenモードで使用します。\n" +
            "これを使うと、サウンド出力の遅延をより小さくできますが、システム負荷は上昇します。",
            "Use WASAPI Event Driven mode.\n" +
            "It reduce sound output lag, but it also decreases system performance.");
        listItems.Add(iSystemWASAPIEventDriven);

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
            () => iSystemASIODevice.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nASIODevice,
            () => CDTXMania.ConfigIni.nASIODevice = iSystemASIODevice.nCurrentlySelectedIndex);
        listItems.Add(iSystemASIODevice);
        
        iSystemReturnToMenu = new CItemBase("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_System
        };
        listItems.Add(iSystemReturnToMenu);

        CacheCurrentSoundDevices();

        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemAudio;
    }
}