using DTXMania.Core;
using DTXMania.UI.Item;
using FDK;

namespace DTXMania;

internal partial class CActConfigList
{
    private void tSetupItemList_AudioDriver()
    {
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
        
        string[] asiodevs = CEnumerateAllAsioDevices.GetAllASIODevices();
        var iSystemASIODevice = new CItemList("ASIO device", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nASIODevice,
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

        // #24820 2013.1.15 yyagi
        var iSystemWASAPIBufferSizeMs = new CItemInteger("WASAPIBufSize", 0, 99999, CDTXMania.ConfigIni.nWASAPIBufferSizeMs,
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
        
        var iSystemWASAPIEventDriven = new CItemToggle("WASAPIEventDriven", CDTXMania.ConfigIni.bEventDrivenWASAPI,
            "WASAPIをEvent Drivenモードで使用します。\n" +
            "これを使うと、サウンド出力の遅延をより小さくできますが、システム負荷は上昇します。",
            "Use WASAPI Event Driven mode.\n" +
            "It reduce sound output lag, but it also decreases system performance.");
        iSystemWASAPIEventDriven.BindConfig(
            () => iSystemWASAPIEventDriven.bON = CDTXMania.ConfigIni.bEventDrivenWASAPI,
            () => CDTXMania.ConfigIni.bEventDrivenWASAPI = iSystemWASAPIEventDriven.bON);
        
        var iSystemSoundTimerType = new CItemToggle("UseOSTimer", CDTXMania.ConfigIni.bUseOSTimer,
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
        
        tRecordToConfigIni();
        listItems.Clear();

        int audioDriver = iSystemAudioDriver.GetIndex();

        switch (audioDriver)
        {
            case 0: //DirectSound
                listItems.Add(iSystemAdjustWaves);
                break;
            
            case 1: //ASIO
                listItems.Add(iSystemASIODevice);
                listItems.Add(iSystemSoundTimerType);
                break;
            
            case 2: //ExclusiveWASAPI
                listItems.Add(iSystemWASAPIBufferSizeMs);
                listItems.Add(iSystemWASAPIEventDriven);
                listItems.Add(iSystemSoundTimerType);
                break;
            
            case 3: //SharedWASAPI
                listItems.Add(iSystemWASAPIBufferSizeMs);
                listItems.Add(iSystemWASAPIEventDriven);
                listItems.Add(iSystemSoundTimerType);
                break;
        }

        //available in all drivers other than DirectSound
        if (audioDriver > 0)
        {
        }

        tAddReturnToMenuItem(tSetupItemList_Audio);
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemAudioDriver;
    }
}