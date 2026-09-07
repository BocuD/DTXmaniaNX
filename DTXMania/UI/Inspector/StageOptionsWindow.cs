using System.Numerics;
using DTXMania.Core;
using DTXMania.SongDb;
using DTXMania.UI.Skin.Preview;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

/// <summary>
/// Whatever the stage on screen needs invented for it: the chart it plays, where its playback sits, the
/// score it shows. A stage with nothing to invent shows nothing.
/// </summary>
public class StageOptionsWindow
{
    public void Draw()
    {
        try
        {
            ImGui.Begin("Stage Options", ImGuiWindowFlags.NoFocusOnAppearing);

            ImGui.TextDisabled(CDTXMania.StageManager.rCurrentStage.eStageID.ToString());
            ImGui.Separator();

            DrawSongSection();
            DrawTransportSection();
            DrawResultSection();
            DrawPreviewSection();
        }
        finally
        {
            ImGui.End();
        }
    }

    #region [ preview mode ]

    private void DrawPreviewSection()
    {
        if (!SkinEditorWindow.Section("Preview"))
        {
            return;
        }

        if (ImGui.Button("Regenerate Song List"))
        {
            SkinPreview.RegenerateSongDb();
        }

        ImGui.SameLine();
        HelpMarker("Builds a new random song list.");

    }

    #endregion

    #region [ song picker ]

    private string songFilter = string.Empty;

    //filter on change, not every frame; libraries get big
    private string cachedFilter = "\0";
    private List<SongNode> matches = [];

    //always offered: the song is picked first, and the stages that need one are opened afterwards
    private void DrawSongSection()
    {
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

        ImGui.TextDisabled(SkinPreview.selectedSong == null
            ? "Select a song"
            : $"Chart: {SkinPreview.selectedSong.title}");
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

        int level = SkinPreview.difficulty;
        ImGui.SetNextItemWidth(120.0f);
        if (ImGui.SliderInt("Difficulty", ref level, 0, 4))
        {
            SkinPreview.difficulty = level;
        }

        if (ImGui.BeginListBox("##PreviewSongs", new Vector2(-1.0f, 180.0f)))
        {
            foreach (SongNode node in matches)
            {
                CChartData? chart = SkinPreview.ChartFor(node, SkinPreview.difficulty);

                ImGui.BeginDisabled(chart == null);
                if (ImGui.Selectable($"{node.title}##{node.path}",
                        ReferenceEquals(node, SkinPreview.selectedSong)))
                {
                    SkinPreview.selectedSong = node;
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

        //not "Score": the section and the score field would share an id, and one of them would stop working
        if (!SkinEditorWindow.Section("Result"))
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

        int rankBefore = stage.nRankValue[instrument];
        bool changed = false;

        changed |= ImGui.DragInt("Total chips", ref entry.nTotalChipsCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Perfect", ref entry.nPerfectCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Great", ref entry.nGreatCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Good", ref entry.nGoodCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Poor", ref entry.nPoorCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Miss", ref entry.nMissCount, 1.0f, 0, 5000);
        changed |= ImGui.DragInt("Max combo", ref entry.nMaxCombo, 1.0f, 0, 5000);

        if (changed)
        {
            PreviewResult.Recalculate(instrument);

            //rank art is built in OnLayoutReady, so it only rebuilds when the rank itself moves
            if (stage.nRankValue[instrument] != rankBefore)
            {
                stage.LoadUI();
            }
        }

        //everything below follows from the counts, the same way it does after a real play
        ImGui.Spacing();
        ImGui.LabelText("Rate", $"{entry.dbPerformanceSkill:0.00}%");
        ImGui.LabelText("Skill", $"{entry.dbGameSkill:0.00}");
        ImGui.LabelText("Rank", RankNames[Math.Clamp(stage.nRankValue[instrument], 0, RankNames.Length - 1)]);
        ImGui.LabelText("Full combo", entry.bIsFullCombo ? "yes" : "no");

        ImGui.Spacing();

        int score = (int)entry.nScore;
        if (ImGui.DragInt("Score", ref score, 250.0f, 0, 1_000_000))
        {
            entry.nScore = score;
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
