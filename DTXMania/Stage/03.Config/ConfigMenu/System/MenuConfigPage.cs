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

        items.Add(SecondsDelay("PreSoundWait", 0, 10000,
            "カーソルが合わされてから\nプレビュー音が鳴り始めるまでの待ち時間。",
            "Delay before the preview sound starts in song select.",
            () => CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs,
            v => CDTXMania.ConfigIni.nSongSelectSoundPreviewWaitTimeMs = v));

        items.Add(SecondsDelay("PreImageWait", 0, 10000,
            "カーソルが合わされてから\nプレビュー画像が表示されるまでの待ち時間。",
            "Delay before the preview image shows in song select.",
            () => CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs,
            v => CDTXMania.ConfigIni.nSongSelectImagePreviewWaitTimeMs = v));

        items.Add(SecondsDelay("ResultDelay", 0, 5000,
            "リザルト画面が表示されるまでの待ち時間。",
            "Delay before the results screen is shown.",
            () => CDTXMania.ConfigIni.nResultDelayMs,
            v => CDTXMania.ConfigIni.nResultDelayMs = v));

        items.Add(SecondsDelay("LoadDelay", 0, 10000,
            "読み込み画面を表示する最小時間。",
            "Minimum time the loading screen is shown.",
            () => CDTXMania.ConfigIni.nLoadingMinMs,
            v => CDTXMania.ConfigIni.nLoadingMinMs = v));

        items.Add(BackItem());
        return items;
    }
}
