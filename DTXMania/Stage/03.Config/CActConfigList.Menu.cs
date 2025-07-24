using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{
    private void tSetupItemList_Menu()
    {
        listItems.Clear();
        
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
        
        CItemToggle iSystemDifficulty = new("Difficulty", CDTXMania.ConfigIni.b難易度表示をXG表示にする,
            "選曲画面での難易度表示方法を変更します。\nON でXG風3ケタ、\nOFF で従来の2ケタ表示になります。",
            "Change difficulty display mode on song selection screen.\n"+
            "ON for XG-style 3-digit display\nOFF for classic 2-digit display.");
        iSystemDifficulty.BindConfig(
            () => iSystemDifficulty.bON = CDTXMania.ConfigIni.b難易度表示をXG表示にする,
            () => CDTXMania.ConfigIni.b難易度表示をXG表示にする = iSystemDifficulty.bON);
        listItems.Add(iSystemDifficulty);
        
        CItemToggle iSystemRandomFromSubBox = new("RandSubBox", CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            "子BOXをRANDOMの対象とする：\nON にすると、RANDOM SELECT 時に、\n子BOXも選択対象とします。",
            "Turn ON to use child BOX (subfolders) at RANDOM SELECT.");
        iSystemRandomFromSubBox.BindConfig(
            () => iSystemRandomFromSubBox.bON = CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            () => CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする = iSystemRandomFromSubBox.bON);
        listItems.Add(iSystemRandomFromSubBox);
            
        CItemInteger iSystemPreviewSoundWait = new("PreSoundWait", 0, 0x2710, CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs,
            "カーソルが合わされてから\n"+
            "プレビュー音が鳴り始める\n"+
            "までの時間を指定します。\n"+
            "0～10000[ms]が指定可能です。",
            "Delay time (ms) to start playing preview sound in song selection screen.\nYou can specify from 0ms to 10000ms.");
        iSystemPreviewSoundWait.BindConfig(
            () => iSystemPreviewSoundWait.nCurrentValue = CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs,
            () => CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs = iSystemPreviewSoundWait.nCurrentValue);
        listItems.Add(iSystemPreviewSoundWait);

        CItemInteger iSystemPreviewImageWait = new("PreImageWait", 0, 0x2710, CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs,
            "カーソルが合わされてから\n"+
            "プレビュー画像が表示\n"+
            "されるまでの時間を\n"+
            "指定します。\n"+
            "0～10000[ms]が指定可能です。",
            "Delay time (ms) to show preview image in song selection screen.\nYou can specify from 0ms to 10000ms.");
        iSystemPreviewImageWait.BindConfig(
            () => iSystemPreviewImageWait.nCurrentValue = CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs,
            () => CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs = iSystemPreviewImageWait.nCurrentValue);
        listItems.Add(iSystemPreviewImageWait);
        
        iSystemReturnToMenu = new CItemBase("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_System
        };
        listItems.Add(iSystemReturnToMenu);
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemMenu;
    }
}