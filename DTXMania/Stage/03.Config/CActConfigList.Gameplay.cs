using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal partial class CActConfigList
{ 
    private void tSetupItemList_Gameplay()
    {
        listItems.Clear();
            
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
        
        CItemList iSystemDamageLevel = new("DamageLevel", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDamageLevel,
            "Miss時のゲージの減少度合い\n"+
            "を指定します。\n"+
            "Risky時は無効となります",
            "Degree of decrease of the damage gauge when missing chips.\nThis setting is ignored when Risky >= 1.",
            ["Small", "Normal", "Large"]);
        iSystemDamageLevel.BindConfig(
            () => iSystemDamageLevel.nCurrentlySelectedIndex = (int)CDTXMania.ConfigIni.eDamageLevel,
            () => CDTXMania.ConfigIni.eDamageLevel = (EDamageLevel)iSystemDamageLevel.nCurrentlySelectedIndex);
        listItems.Add(iSystemDamageLevel);

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
            
        CItemList iSystemSkillMode = new("SkillMode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSkillMode,
            "達成率、スコアの計算方法を変更します。\n" +
            "CLASSIC:V6までのスコア計算とV8までの\n" +
            "ランク計算です。\n" +
            "XG:XGシリーズの達成率計算とV7以降の\n" +
            "スコア計算です。",
            "Skill rate and score calculation method\nCLASSIC: Pre-V6 score calculation and pre-V8 rank calculation\nXG: Current score and rank calculation",
            ["CLASSIC", "XG"]);
        iSystemSkillMode.BindConfig(
            () => iSystemSkillMode.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nSkillMode,
            () => CDTXMania.ConfigIni.nSkillMode = iSystemSkillMode.nCurrentlySelectedIndex);
        listItems.Add(iSystemSkillMode);

        CItemToggle iSystemClassicNotes = new("CLASSIC Notes", CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする,
            "CLASSIC譜面の判別の有無を設定します。\n",
            "Use CLASSIC score calculation when a classic song is detected.\n");
        iSystemClassicNotes.BindConfig(
            () => iSystemClassicNotes.bON = CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする,
            () => CDTXMania.ConfigIni.bCLASSIC譜面判別を有効にする = iSystemClassicNotes.bON);
        listItems.Add(iSystemClassicNotes);
            
        CItemToggle iAutoAddGage = new("AutoAddGage", CDTXMania.ConfigIni.bAutoAddGage,
            "ONの場合、AUTO判定も\n"+
            "ゲージに加算されます。\n",
            "If ON, will be added to the judgment also gauge AUTO.\n" +
            "");
        iAutoAddGage.BindConfig(
            () => iAutoAddGage.bON = CDTXMania.ConfigIni.bAutoAddGage,
            () => CDTXMania.ConfigIni.bAutoAddGage = iAutoAddGage.bON);
        listItems.Add(iAutoAddGage);
        
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
            ["OFF", "ON", "GREAT-"]);
        iSystemShowLag.BindConfig(
            () => iSystemShowLag.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nShowLagType,
            () => CDTXMania.ConfigIni.nShowLagType = iSystemShowLag.nCurrentlySelectedIndex);
        listItems.Add(iSystemShowLag);

        CItemList iSystemShowLagColor = new("ShowLagTimeColor", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nShowLagTypeColor,
            "ズレ時間表示の表示色変更：\n  TYPE-A: 早ズレを青、遅ズレを赤で表示します。\n  TYPE-B: 早ズレを赤、遅ズレを青で表示します。",
            "Change color of lag time display：\nTYPE-A: early notes in blue and late notes in red.\nTYPE-B: early notes in red and late notes in blue.",
            ["TYPE-A", "TYPE-B"]);
        iSystemShowLagColor.BindConfig(
            () => iSystemShowLagColor.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nShowLagTypeColor,
            () => CDTXMania.ConfigIni.nShowLagTypeColor = iSystemShowLagColor.nCurrentlySelectedIndex);
        listItems.Add(iSystemShowLagColor);

        CItemToggle iSystemShowLagHitCount = new("ShowLagHitCount", CDTXMania.ConfigIni.bShowLagHitCount,
            "ズレヒット数表示:\n演奏と結果画面に早ズレ、遅ズレヒット数で表示する場合はONにします", //fisyher: Constructed using DeepL, feedback welcomed to improved accuracy
            "ShowLagHitCount:\nTurn ON to display Early/Late Hit Counters in Performance and Result Screen.");
        iSystemShowLagHitCount.BindConfig(
            () => iSystemShowLagHitCount.bON = CDTXMania.ConfigIni.bShowLagHitCount,
            () => CDTXMania.ConfigIni.bShowLagHitCount = iSystemShowLagHitCount.bON);
        listItems.Add(iSystemShowLagHitCount);

        CItemToggle iSystemMetronome = new("Metronome", CDTXMania.ConfigIni.bMetronome,
            "メトロノームを有効にします。", "Enable Metronome.");
        iSystemMetronome.BindConfig(
            () => iSystemMetronome.bON = CDTXMania.ConfigIni.bMetronome,
            () => CDTXMania.ConfigIni.bMetronome = iSystemMetronome.bON);
        listItems.Add(iSystemMetronome);
        
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
        
        iSystemReturnToMenu = new CItemBase("<< Return To Menu", CItemBase.EPanelType.Other,
            "左側のメニューに戻ります。",
            "Return to left menu.")
        {
            action = tSetupItemList_System
        };
        listItems.Add(iSystemReturnToMenu);
        
        InitializeList();
        nCurrentSelection = 0;
        eMenuType = EMenuType.SystemGameplay;
    }
}