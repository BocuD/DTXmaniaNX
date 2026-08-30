using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Skin.Preview;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// The skin editor's Stage tab: which stage is on screen, and whatever that stage needs invented for it.
/// </summary>
public class SkinPreviewPanel
{
    internal void DrawContents()
    {
        if (!SkinPreview.IsActive)
        {
            if (ImGui.Button("Enter Preview Mode"))
            {
                SkinPreview.Enter();
            }

            return;
        }

        DrawStageSection();
        DrawSongSection();
        DrawTransportSection();
        DrawResultSection();
    }

    #region [ stage ]

    private int stageIndex;

    //follow the game when it moves itself, but do not overwrite a choice that has not been used yet
    private CStage.EStage lastSeenStage = CStage.EStage.DoNothing_0;

    private void DrawStageSection()
    {
        if (!SkinEditorWindow.Section("Stage"))
        {
            return;
        }

        CStage current = CDTXMania.StageManager.rCurrentStage;

        if (current.eStageID != lastSeenStage)
        {
            lastSeenStage = current.eStageID;

            int index = Array.IndexOf(SkinPreview.Stages, current.eStageID);
            if (index >= 0)
            {
                stageIndex = index;
            }
        }

        string[] names = SkinPreview.Stages.Select(stage => stage.ToString()).ToArray();

        ImGui.SetNextItemWidth(220.0f);
        ImGui.Combo("##PreviewStage", ref stageIndex, names, names.Length);

        CStage.EStage wanted = SkinPreview.Stages[stageIndex];

        //these stages cannot open without a chart
        bool needsSong = SkinPreview.RequiresSong(wanted);
        bool blocked = needsSong && SelectedChart == null;

        ImGui.SameLine();
        ImGui.BeginDisabled(blocked);
        if (ImGui.Button("Load Stage"))
        {
            SkinPreview.LoadStage(wanted, selectedSong, SelectedChart, difficulty);
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(blocked ? "pick a song first" : $"showing {current.eStageID}");

        bool hold = SkinPreview.HoldStage;
        if (ImGui.Checkbox("Hold this stage", ref hold))
        {
            SkinPreview.HoldStage = hold;
        }

        ImGui.SameLine();
        HelpMarker("Prevent stage transitions (eg, because of timers or keyboard input) by turning this on");

        if (ImGui.Button("Regenerate Song List"))
        {
            SkinPreview.RegenerateSongDb();
        }

        ImGui.SameLine();
        HelpMarker("Generate a random song list");

        ImGui.SameLine();
        if (ImGui.Button("Exit Preview"))
        {
            SkinPreview.Exit();
        }
    }

    private bool WantsSong => SkinPreview.RequiresSong(SkinPreview.Stages[stageIndex]);

    #endregion

    #region [ song picker ]

    private string songFilter = string.Empty;
    private int difficulty;

    private SongNode? selectedSong;

    //derived, so changing difficulty after picking a song still works
    private CChartData? SelectedChart => selectedSong == null ? null : ChartFor(selectedSong, difficulty);

    //filter on change, not every frame; libraries get big
    private string cachedFilter = "\0";
    private List<SongNode> matches = [];

    private void DrawSongSection()
    {
        if (!WantsSong)
        {
            return;
        }

        if (!SkinEditorWindow.Section("Song"))
        {
            return;
        }

        SongDb.SongDb library = CDTXMania.SongDb;

        if (library is not { hasEverScanned: true } || library.flattenedSongList.Count == 0)
        {
            ImGui.TextWrapped("No songs detected");
            return;
        }

        ImGui.TextDisabled(selectedSong == null
            ? "Select a song"
            : $"Chart: {selectedSong.title}");

        ImGui.SetNextItemWidth(220.0f);
        ImGui.InputTextWithHint("##PreviewSongFilter", "Search songs", ref songFilter, 128);

        if (songFilter != cachedFilter)
        {
            cachedFilter = songFilter;
            matches = library.flattenedSongList
                .Where(node => node.nodeType == SongNode.ENodeType.SONG
                               && node.title.Contains(songFilter, StringComparison.OrdinalIgnoreCase))
                .Take(MaxMatches)
                .ToList();
        }

        ImGui.SetNextItemWidth(120.0f);
        ImGui.SliderInt("Difficulty", ref difficulty, 0, 4);

        if (ImGui.BeginListBox("##PreviewSongs", new Vector2(-1.0f, 180.0f)))
        {
            foreach (SongNode node in matches)
            {
                CChartData? chart = ChartFor(node, difficulty);

                ImGui.BeginDisabled(chart == null);
                if (ImGui.Selectable($"{node.title}##{node.path}", ReferenceEquals(node, selectedSong)))
                {
                    selectedSong = node;
                }
                ImGui.EndDisabled();
            }

            ImGui.EndListBox();
        }

        ImGui.TextDisabled(matches.Count >= MaxMatches
            ? $"First {MaxMatches} matches"
            : $"{matches.Count} songs");
    }

    private const int MaxMatches = 200;

    //falls back to any chart the song does have
    private static CChartData? ChartFor(SongNode song, int difficulty)
        => song.charts[difficulty] ?? song.charts.FirstOrDefault(chart => chart != null);

    #endregion

    #region [ transport ]

    private void DrawTransportSection()
    {
        if (CDTXMania.StageManager.rCurrentStage is not CStagePerfCommonScreen screen)
        {
            return;
        }

        if (!SkinEditorWindow.Section("Playback"))
        {
            return;
        }

        long length = screen.PreviewLengthMs;

        //show the drag position, not the playhead, or it fights the pointer
        int position = scrubbing ? scrubTarget : (int)screen.PreviewPositionMs;

        ImGui.Text($"{Timecode(position)} / {Timecode(length)}");

        ImGui.SetNextItemWidth(-1.0f);
        if (ImGui.SliderInt("##PreviewScrub", ref position, 0, (int)Math.Max(1, length), Timecode(position)))
        {
            scrubbing = true;
            scrubTarget = position;
        }

        //seek on release; seeking mid-drag re-cues every sound on the way
        if (scrubbing && ImGui.IsItemDeactivatedAfterEdit())
        {
            scrubbing = false;
            screen.tJumpInSong(scrubTarget);
        }

        if (ImGui.Button("Restart"))
        {
            screen.tRestartForPreview();
        }

        ImGui.SameLine();
        if (ImGui.Button($"-{CDTXMania.ConfigIni.nSkipTimeMs / 1000}s"))
        {
            screen.tJumpInSong(screen.PreviewPositionMs - CDTXMania.ConfigIni.nSkipTimeMs);
        }

        ImGui.SameLine();
        if (ImGui.Button($"+{CDTXMania.ConfigIni.nSkipTimeMs / 1000}s"))
        {
            screen.tJumpInSong(screen.PreviewPositionMs + CDTXMania.ConfigIni.nSkipTimeMs);
        }

        ImGui.SameLine();
        bool paused = screen.PreviewPaused;
        if (ImGui.Button(paused ? "Resume" : "Pause"))
        {
            screen.PreviewPaused = !paused;
        }

        ImGui.SetNextItemWidth(120.0f);
        ImGui.DragInt("Jump to bar", ref bar, 0.2f, 0, 999);

        ImGui.SameLine();
        if (ImGui.Button("Go"))
        {
            screen.tJumpInSongToBar(bar);
        }

        DrawSpeed(screen);
        DrawLoop(screen);
    }

    private bool scrubbing;
    private int scrubTarget;
    private int bar;

    private static void DrawSpeed(CStagePerfCommonScreen screen)
    {
        ImGui.Text($"Speed: {CDTXMania.ConfigIni.nPlaySpeed / 20.0:0.00}x");

        ImGui.SameLine();
        ImGui.BeginDisabled(CDTXMania.ConfigIni.nPlaySpeed <= CConstants.PLAYSPEED_MIN);
        if (ImGui.Button("-##PreviewSpeed"))
        {
            screen.PreviewChangeSpeed(-1);
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(CDTXMania.ConfigIni.nPlaySpeed >= CConstants.PLAYSPEED_MAX);
        if (ImGui.Button("+##PreviewSpeed"))
        {
            screen.PreviewChangeSpeed(1);
        }
        ImGui.EndDisabled();
    }

    private static void DrawLoop(CStagePerfCommonScreen screen)
    {
        bool looping = screen.PreviewLoopEndMs != -1;

        ImGui.Text(looping
            ? $"Loop: {Timecode(screen.PreviewLoopBeginMs)} - {Timecode(screen.PreviewLoopEndMs)}"
            : "Loop: none");

        ImGui.SameLine();
        if (ImGui.Button("Loop 10s Here"))
        {
            long begin = screen.PreviewPositionMs;
            screen.PreviewSetLoop(begin, begin + 10000);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!looping);
        if (ImGui.Button("Clear Loop"))
        {
            screen.PreviewClearLoop();
        }
        ImGui.EndDisabled();
    }

    private static string Timecode(long milliseconds)
        => TimeSpan.FromMilliseconds(Math.Max(0, milliseconds)).ToString(@"m\:ss");

    #endregion

    #region [ result ]

    private static readonly string[] RankNames = ["SS", "S", "A", "B", "C", "D", "E"];

    private void DrawResultSection()
    {
        if (CDTXMania.StageManager.rCurrentStage is not CStageResult stage)
        {
            return;
        }

        if (!SkinEditorWindow.Section("Score"))
        {
            return;
        }

        int instrument = CDTXMania.GetCurrentInstrument();
        CScoreIni.CPerformanceEntry entry = stage.stPerformanceEntry[instrument];

        foreach (PreviewResult.Preset preset in Enum.GetValues<PreviewResult.Preset>())
        {
            if (ImGui.Button(preset.ToString()))
            {
                PreviewResult.Apply(instrument, preset);
                stage.LoadUI();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();

        //read back live through ResultData, so the numbers move as they are dragged
        ImGui.DragInt("Total chips", ref entry.nTotalChipsCount, 1.0f, 0, 5000);
        ImGui.DragInt("Perfect", ref entry.nPerfectCount, 1.0f, 0, 5000);
        ImGui.DragInt("Great", ref entry.nGreatCount, 1.0f, 0, 5000);
        ImGui.DragInt("Good", ref entry.nGoodCount, 1.0f, 0, 5000);
        ImGui.DragInt("Poor", ref entry.nPoorCount, 1.0f, 0, 5000);
        ImGui.DragInt("Miss", ref entry.nMissCount, 1.0f, 0, 5000);
        ImGui.DragInt("Max combo", ref entry.nMaxCombo, 1.0f, 0, 5000);

        ImGui.TextDisabled(entry.bIsFullCombo
            ? "Full combo: max combo matches the judgement total"
            : "Not a full combo");

        //no double widget, and two decimals is all that shows
        float skill = (float)entry.dbGameSkill;
        if (ImGui.DragFloat("Skill", ref skill, 0.05f, 0.0f, 999.0f))
        {
            entry.dbGameSkill = skill;
        }

        float rate = (float)entry.dbPerformanceSkill;
        if (ImGui.DragFloat("Rate %%", ref rate, 0.1f, 0.0f, 100.0f))
        {
            entry.dbPerformanceSkill = rate;
        }

        int score = (int)entry.nScore;
        if (ImGui.DragInt("Score", ref score, 250.0f, 0, 1_000_000))
        {
            entry.nScore = score;
        }

        //rank art is built in OnLayoutReady, so reload the UI
        int rank = Math.Clamp(stage.nRankValue[instrument], 0, RankNames.Length - 1);
        ImGui.SetNextItemWidth(120.0f);
        if (ImGui.Combo("Rank", ref rank, RankNames, RankNames.Length))
        {
            stage.nRankValue[instrument] = rank;
            stage.LoadUI();
        }

        bool newRecord = stage.bNewRecordSkill[instrument];
        if (ImGui.Checkbox("New record", ref newRecord))
        {
            stage.bNewRecordSkill[instrument] = newRecord;
            stage.bNewRecordScore[instrument] = newRecord;
            stage.bNewRecordRank[instrument] = newRecord;
        }
    }

    #endregion

    internal static void HelpMarker(string text)
    {
        ImGui.TextDisabled("(?)");

        if (!ImGui.IsItemHovered())
        {
            return;
        }

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28.0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
