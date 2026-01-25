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
        
        CItemList iSystemDifficltyDisplay = new("Difficulty Display", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSkillMode,
            "選曲画面での難易度表示方法を変更します。\n" +
            "CLASSIC: 2桁表示\n" +
            "XG: 3桁表示",
            "Change difficulty display mode on song selection screen.\n" +
            "CLASSIC: 2-digit display.\n"+
            "XG: 3-digit display",
            ["CLASSIC", "XG"]);
        iSystemDifficltyDisplay.BindConfig(
            () => iSystemDifficltyDisplay.nCurrentlySelectedIndex = CDTXMania.ConfigIni.bDisplayDifficultyXGStyle ? 1 : 0,
            () => CDTXMania.ConfigIni.bDisplayDifficultyXGStyle = iSystemDifficltyDisplay.nCurrentlySelectedIndex == 1);
        listItems.Add(iSystemDifficltyDisplay);
        
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
        
        tAddReturnToMenuItem(tSetupItemList_System);
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemMenu;
    }
}