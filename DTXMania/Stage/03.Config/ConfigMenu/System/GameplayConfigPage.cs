using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Item;

namespace DTXMania;

internal sealed class GameplayConfigPage : ConfigPage
{
    public GameplayConfigPage(ConfigList list) : base(list)
    {
    }

    public override List<CItemBase> Build()
    {
        List<CItemBase> items = [];

        CItemInteger risky = new("Risky", 0, 10, CDTXMania.ConfigIni.nRisky,
            "設定した回数分\nミスをすると、強制的に\nSTAGE FAILEDになります。",
            "Risky mode:\nNumber of mistakes (Poor/Miss) before getting STAGE FAILED.\nSet 0 to disable Risky mode.");
        risky.BindConfig(
            () => risky.nCurrentValue = CDTXMania.ConfigIni.nRisky,
            () => CDTXMania.ConfigIni.nRisky = risky.nCurrentValue);
        items.Add(risky);

        CItemList damageLevel = new("DamageLevel", CItemBase.EPanelType.Normal, (int)CDTXMania.ConfigIni.eDamageLevel,
            "Miss時のゲージの減少度合い\nを指定します。\nRisky時は無効となります",
            "Degree of decrease of the damage gauge when missing chips.\nThis setting is ignored when Risky >= 1.",
            ["Small", "Normal", "Large"]);
        damageLevel.BindConfig(
            () => damageLevel.nCurrentlySelectedIndex = (int)CDTXMania.ConfigIni.eDamageLevel,
            () => CDTXMania.ConfigIni.eDamageLevel = (EDamageLevel)damageLevel.nCurrentlySelectedIndex);
        items.Add(damageLevel);

        CItemInteger playSpeed = new("PlaySpeed", CConstants.PLAYSPEED_MIN, CConstants.PLAYSPEED_MAX, CDTXMania.ConfigIni.nPlaySpeed,
            "曲の演奏速度を、速くしたり\n遅くしたりすることができます。",
            "Change the song speed.\nFor example, set PlaySpeed = 0.500 for half speed.\nNote: It also changes the song's pitch.");
        playSpeed.BindConfig(
            () => playSpeed.nCurrentValue = CDTXMania.ConfigIni.nPlaySpeed,
            () => CDTXMania.ConfigIni.nPlaySpeed = playSpeed.nCurrentValue);
        // displayed as a decimal multiplier (value / 20), matching the old renderer's special case
        playSpeed.formatValue = () => (playSpeed.nCurrentValue / 20.0).ToString("0.000");
        items.Add(playSpeed);

        CItemList skillMode = new("SkillMode", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nSkillMode,
            "達成率、スコアの計算方法を変更します。\nCLASSIC:V6までのスコア計算とV8までの\nランク計算です。\nXG:XGシリーズの達成率計算とV7以降の\nスコア計算です。",
            "Skill rate and score calculation method\nCLASSIC: Pre-V6 score calculation and pre-V8 rank calculation\nXG: Current score and rank calculation",
            ["CLASSIC", "XG"]);
        skillMode.BindConfig(
            () => skillMode.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nSkillMode,
            () => CDTXMania.ConfigIni.nSkillMode = skillMode.nCurrentlySelectedIndex);
        items.Add(skillMode);

        CItemToggle classicNotes = new("CLASSIC Notes", CDTXMania.ConfigIni.bClassicScoreDisplay,
            "CLASSIC譜面の判別の有無を設定します。\n",
            "Use CLASSIC score calculation when a classic song is detected.\n");
        classicNotes.BindConfig(
            () => classicNotes.bON = CDTXMania.ConfigIni.bClassicScoreDisplay,
            () => CDTXMania.ConfigIni.bClassicScoreDisplay = classicNotes.bON);
        items.Add(classicNotes);

        CItemToggle autoAddGage = new("AutoAddGage", CDTXMania.ConfigIni.bAutoAddGage,
            "ONの場合、AUTO判定も\nゲージに加算されます。\n",
            "If ON, AUTO judgements are added to the gauge.\n");
        autoAddGage.BindConfig(
            () => autoAddGage.bON = CDTXMania.ConfigIni.bAutoAddGage,
            () => CDTXMania.ConfigIni.bAutoAddGage = autoAddGage.bON);
        items.Add(autoAddGage);

        CItemToggle stageFailed = new("StageFailed", CDTXMania.ConfigIni.bSTAGEFAILEDEnabled,
            "ONにするとゲージが\nなくなった時にSTAGE FAILEDとなり演奏が中断されます。",
            "Turn OFF if you don't want to encounter STAGE FAILED.");
        stageFailed.BindConfig(
            () => stageFailed.bON = CDTXMania.ConfigIni.bSTAGEFAILEDEnabled,
            () => CDTXMania.ConfigIni.bSTAGEFAILEDEnabled = stageFailed.bON);
        items.Add(stageFailed);

        CItemToggle showScore = new("ShowScore", CDTXMania.ConfigIni.bShowScore,
            "演奏中のスコアの表示の有無を設定します。",
            "Display the score during the game.");
        showScore.BindConfig(
            () => showScore.bON = CDTXMania.ConfigIni.bShowScore,
            () => CDTXMania.ConfigIni.bShowScore = showScore.bON);
        items.Add(showScore);

        CItemToggle showMusicInfo = new("ShowMusicInfo", CDTXMania.ConfigIni.bShowMusicInfo,
            "OFFにすると演奏中のジャケット、曲情報を\n表示しません。",
            "When turned OFF, the cover and song information being played are not displayed.");
        showMusicInfo.BindConfig(
            () => showMusicInfo.bON = CDTXMania.ConfigIni.bShowMusicInfo,
            () => CDTXMania.ConfigIni.bShowMusicInfo = showMusicInfo.bON);
        items.Add(showMusicInfo);

        CItemToggle stageEffect = new("StageEffect", CDTXMania.ConfigIni.DisplayBonusEffects,
            "OFFにすると、\nゲーム中の背景演出が\n非表示になります。",
            "When turned off, background stage effects are disabled");
        stageEffect.BindConfig(
            () => stageEffect.bON = CDTXMania.ConfigIni.DisplayBonusEffects,
            () => CDTXMania.ConfigIni.DisplayBonusEffects = stageEffect.bON);
        items.Add(stageEffect);

        CItemList showLag = new("ShowLagTime", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nShowLagType,
            "ズレ時間表示：\nジャストタイミングからの\nズレ時間(ms)を表示します。\nOFF: 表示しません。\nON: ズレ時間を表示します。\nGREAT-: PERFECT以外の時\nのみ表示します。",
            "Display the lag from ideal hit time (ms)\nOFF: Don't show.\nON: Show.\nGREAT-: Show except for perfect chips.",
            ["OFF", "ON", "GREAT-"]);
        showLag.BindConfig(
            () => showLag.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nShowLagType,
            () => CDTXMania.ConfigIni.nShowLagType = showLag.nCurrentlySelectedIndex);
        items.Add(showLag);

        CItemList showLagColor = new("ShowLagTimeColor", CItemBase.EPanelType.Normal, CDTXMania.ConfigIni.nShowLagTypeColor,
            "ズレ時間表示の表示色変更：\n  TYPE-A: 早ズレを青、遅ズレを赤で表示します。\n  TYPE-B: 早ズレを赤、遅ズレを青で表示します。",
            "Change color of lag time display：\nTYPE-A: early notes in blue and late notes in red.\nTYPE-B: early notes in red and late notes in blue.",
            ["TYPE-A", "TYPE-B"]);
        showLagColor.BindConfig(
            () => showLagColor.nCurrentlySelectedIndex = CDTXMania.ConfigIni.nShowLagTypeColor,
            () => CDTXMania.ConfigIni.nShowLagTypeColor = showLagColor.nCurrentlySelectedIndex);
        items.Add(showLagColor);

        CItemToggle showLagHitCount = new("ShowLagHitCount", CDTXMania.ConfigIni.bShowLagHitCount,
            "ズレヒット数表示:\n演奏と結果画面に早ズレ、遅ズレヒット数で表示する場合はONにします",
            "ShowLagHitCount:\nTurn ON to display Early/Late Hit Counters in Performance and Result Screen.");
        showLagHitCount.BindConfig(
            () => showLagHitCount.bON = CDTXMania.ConfigIni.bShowLagHitCount,
            () => CDTXMania.ConfigIni.bShowLagHitCount = showLagHitCount.bON);
        items.Add(showLagHitCount);

        CItemToggle metronome = new("Metronome", CDTXMania.ConfigIni.bMetronome,
            "メトロノームを有効にします。", "Enable Metronome.");
        metronome.BindConfig(
            () => metronome.bON = CDTXMania.ConfigIni.bMetronome,
            () => CDTXMania.ConfigIni.bMetronome = metronome.bON);
        items.Add(metronome);

        CItemToggle saveScore = new("SaveScore", CDTXMania.ConfigIni.bScoreIniを出力する,
            "演奏記録の保存：\nONで演奏記録を.score.iniに\n保存します。\n",
            "Turn ON to save high scores/skills.\nTurn OFF in case your song data are on read-only media.");
        saveScore.BindConfig(
            () => saveScore.bON = CDTXMania.ConfigIni.bScoreIniを出力する,
            () => CDTXMania.ConfigIni.bScoreIniを出力する = saveScore.bON);
        items.Add(saveScore);

        CItemToggle autoResultCapture = new("AutoSaveResult", CDTXMania.ConfigIni.bIsAutoResultCapture,
            "ONにすると、NewRecord時に\n自動でリザルト画像を\n曲データと同じフォルダに\n保存します。",
            "AutoSaveResult:\nTurn ON to save your result screen image automatically when you get a hiscore/hiskill.");
        autoResultCapture.BindConfig(
            () => autoResultCapture.bON = CDTXMania.ConfigIni.bIsAutoResultCapture,
            () => CDTXMania.ConfigIni.bIsAutoResultCapture = autoResultCapture.bON);
        items.Add(autoResultCapture);

        items.Add(BackItem());
        return items;
    }
}
