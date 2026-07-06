using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class MenuConfigPage : ConfigPage
{
    public MenuConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        CItemToggle musicNameDispDef = new("MusicNameDispDEF", CDTXMania.ConfigIni.b曲名表示をdefのものにする,
            "表示される曲名をdefのものにします。\nただし選曲画面の表示は、\ndefファイルの曲名が\n優先されます。",
            "Display the music title from SET.def file");
        musicNameDispDef.BindConfig(
            () => musicNameDispDef.bON = CDTXMania.ConfigIni.b曲名表示をdefのものにする,
            () => CDTXMania.ConfigIni.b曲名表示をdefのものにする = musicNameDispDef.bON);
        items.Add(musicNameDispDef);

        CItemList difficultyDisplay = new("Difficulty Display", CItemBase.EPanelType.Normal,
            CDTXMania.ConfigIni.bDisplayDifficultyXGStyle ? 1 : 0,
            "選曲画面での難易度表示方法を変更します。\nCLASSIC: 2桁表示\nXG: 3桁表示",
            "Change difficulty display mode on song selection screen.\nCLASSIC: 2-digit display.\nXG: 3-digit display",
            ["CLASSIC", "XG"]);
        difficultyDisplay.BindConfig(
            () => difficultyDisplay.nCurrentlySelectedIndex = CDTXMania.ConfigIni.bDisplayDifficultyXGStyle ? 1 : 0,
            () => CDTXMania.ConfigIni.bDisplayDifficultyXGStyle = difficultyDisplay.nCurrentlySelectedIndex == 1);
        items.Add(difficultyDisplay);

        CItemToggle randomFromSubBox = new("RandSubBox", CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            "子BOXをRANDOMの対象とする：\nON にすると、RANDOM SELECT 時に、\n子BOXも選択対象とします。",
            "Turn ON to use child BOX (subfolders) at RANDOM SELECT.");
        randomFromSubBox.BindConfig(
            () => randomFromSubBox.bON = CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
            () => CDTXMania.ConfigIni.bランダムセレクトで子BOXを検索対象とする = randomFromSubBox.bON);
        items.Add(randomFromSubBox);

        CItemInteger previewSoundWait = new("PreSoundWait", 0, 0x2710, CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs,
            "カーソルが合わされてから\nプレビュー音が鳴り始める\nまでの時間を指定します。\n0～10000[ms]が指定可能です。",
            "Delay time (ms) to start playing preview sound in song selection screen.\nYou can specify from 0ms to 10000ms.");
        previewSoundWait.BindConfig(
            () => previewSoundWait.nCurrentValue = CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs,
            () => CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs = previewSoundWait.nCurrentValue);
        items.Add(previewSoundWait);

        CItemInteger previewImageWait = new("PreImageWait", 0, 0x2710, CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs,
            "カーソルが合わされてから\nプレビュー画像が表示\nされるまでの時間を\n指定します。\n0～10000[ms]が指定可能です。",
            "Delay time (ms) to show preview image in song selection screen.\nYou can specify from 0ms to 10000ms.");
        previewImageWait.BindConfig(
            () => previewImageWait.nCurrentValue = CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs,
            () => CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs = previewImageWait.nCurrentValue);
        items.Add(previewImageWait);

        items.Add(BackItem());
        return items;
    }
}
