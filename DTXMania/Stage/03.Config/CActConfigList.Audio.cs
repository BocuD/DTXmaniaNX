using DTXMania.Core;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal partial class CActConfigList
{
    private int iSystemSoundType_initial;
    private int iSystemWASAPIBufferSizeMs_initial;
    private int iSystemASIODevice_initial;
    private bool bSystemSoundTimerType_initial;
    private CItemInteger iSystemMasterVolume;
    private CItemToggle iSystemTimeStretch;

    private bool audioMenuOpened;
    
    private CItemList iSystemAudioDriver;

    private void CacheCurrentSoundDevices()
    {
        iSystemSoundType_initial = CDTXMania.ConfigIni.nSoundDriverType; // CONFIGに入ったときの値を保持しておく
        iSystemWASAPIBufferSizeMs_initial = CDTXMania.ConfigIni.nWASAPIBufferSizeMs; // CONFIG脱出時にこの値から変更されているようなら
        iSystemASIODevice_initial = CDTXMania.ConfigIni.nASIODevice;
        bSystemSoundTimerType_initial = CDTXMania.ConfigIni.bUseOSTimer;
    }
    
    private void HandleSoundDeviceChanges()
    {
        if (!audioMenuOpened)
        {
            return;
        }

        //reset for next load
        audioMenuOpened = false;

        if (iSystemSoundType_initial != CDTXMania.ConfigIni.nSoundDriverType ||
            iSystemWASAPIBufferSizeMs_initial != CDTXMania.ConfigIni.nWASAPIBufferSizeMs ||
            iSystemASIODevice_initial != CDTXMania.ConfigIni.nASIODevice ||
            bSystemSoundTimerType_initial != CDTXMania.ConfigIni.bUseOSTimer)
        {
            ESoundDeviceType soundDeviceType;
            switch (CDTXMania.ConfigIni.nSoundDriverType)
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

            CDTXMania.SoundManager.tInitialize(soundDeviceType,
                CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
                false,
                0,
                CDTXMania.ConfigIni.nASIODevice,
                CDTXMania.ConfigIni.bUseOSTimer);

            CDTXMania.app.AddSoundTypeToWindowTitle();
        }
        
        CSoundManager.bIsTimeStretch = iSystemTimeStretch.bON;
    }
    
    private void tSetupItemList_Audio()
    {
        tRecordToConfigIni();
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
        CItemInteger iSystemBGMAdjust = new( "BGM Offset", -99, 99, CDTXMania.ConfigIni.nCommonBGMAdjustMs,
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
        
        // #24820 2013.1.3 yyagi
        iSystemAudioDriver = new CItemList("Audio Driver", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSoundDriverType,
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
        iSystemAudioDriver.BindConfig(
            () => iSystemAudioDriver.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nSoundDriverType,
            () => CDTXMania.ConfigIni.nSoundDriverType = iSystemAudioDriver.nCurrentlySelectedIndex);
        listItems.Add(iSystemAudioDriver);

        CItemBase iSystemGoToAudioDriver = new("Audio Driver Options", CItemBase.EPanelType.Folder,
            "システムのオーディオドライバー設定に関する項目を設定します。",
            "Open the audio driver settings sub menu.")
        {
            action = tSetupItemList_AudioDriver
        };
        listItems.Add(iSystemGoToAudioDriver);
        
        tAddReturnToMenuItem(tSetupItemList_System);

        audioMenuOpened = true;
        CacheCurrentSoundDevices();
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemAudio;
    }
}