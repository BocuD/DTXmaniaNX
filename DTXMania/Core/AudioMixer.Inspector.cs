using System.Numerics;
using Hexa.NET.ImGui;

namespace DTXMania.Core;

public static partial class AudioMixer
{
    private static readonly Vector4 Busy = new(0.9f, 0.65f, 0.3f, 1.0f);

    public static void DrawWindow()
    {
        if (!ImGui.Begin("Audio Mixer"))
        {
            ImGui.End();
            return;
        }

        DrawTotals();
        ImGui.Separator();

        if (ImGui.Button("Stop Everything"))
        {
            foreach (CSystemSound clip in clips.Keys.ToArray())
            {
                Stop(clip);
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"channels grow on demand, guard at {RunawayGuard}");

        DrawClips();

        ImGui.End();
    }

    private static void DrawTotals()
    {
        int channels = 0;
        int sounding = 0;

        foreach (Clip state in clips.Values)
        {
            channels += state.voices.Count;
            sounding += Sounding(state);
        }

        ImGui.Text($"Clips {clips.Count}     Channels {channels}     Sounding {sounding}");

        ImGui.TextDisabled(CSystemSound.rLastPlayedExclusiveSystemSound is { } exclusive
            ? $"exclusive: {exclusive.strFilename}"
            : "exclusive: none");
    }

    private static void DrawClips()
    {
        if (clips.Count == 0)
        {
            ImGui.TextDisabled("Nothing loaded.");
            return;
        }

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                                                              | ImGuiTableFlags.SizingStretchProp
                                                              | ImGuiTableFlags.ScrollY;

        if (!ImGui.BeginTable("mixerClips", 5, flags))
        {
            return;
        }

        ImGui.TableSetupColumn("Clip", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthFixed, 120.0f);
        ImGui.TableSetupColumn("Plays", ImGuiTableColumnFlags.WidthFixed, 60.0f);
        ImGui.TableSetupColumn("Flags", ImGuiTableColumnFlags.WidthFixed, 110.0f);
        ImGui.TableSetupColumn("Voices", ImGuiTableColumnFlags.WidthFixed, 80.0f);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        //by name, so a row stays where it was rather than moving as counts change
        foreach ((CSystemSound clip, Clip state) in clips.OrderBy(entry => entry.Key.strFilename))
        {
            DrawClipRow(clip, state);
        }

        ImGui.EndTable();
    }

    private static void DrawClipRow(CSystemSound clip, Clip state)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.Text(clip.strFilename.Length > 0 ? clip.strFilename : "(unnamed)");

        //how much of the pool is in use. Full means the next play has to grow it, which is the one moment
        //worth noticing: it is when a decode happens
        ImGui.TableNextColumn();
        int sounding = Sounding(state);
        bool saturated = sounding > 0 && sounding == state.voices.Count;

        if (saturated)
        {
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Busy);
        }

        float used = state.voices.Count == 0 ? 0.0f : sounding / (float)state.voices.Count;
        ImGui.ProgressBar(used, new Vector2(-1.0f, 0.0f), $"{sounding}/{state.voices.Count}");

        if (saturated)
        {
            ImGui.PopStyleColor();
        }

        ImGui.TableNextColumn();
        ImGui.Text(state.plays.ToString());

        ImGui.TableNextColumn();
        ImGui.TextDisabled(Flags(clip, state));

        ImGui.TableNextColumn();
        ImGui.TextDisabled(state.audio?.VoiceKind ?? "-");
    }

    private static int Sounding(Clip state)
    {
        int count = 0;

        foreach (Voice voice in state.voices)
        {
            if (voice.sound.IsPlaying)
            {
                count++;
            }
        }

        return count;
    }

    private static string Flags(CSystemSound clip, Clip state)
    {
        string flags = string.Empty;

        if (clip.loop)
        {
            flags += "loop ";
        }

        if (clip.bExclusive)
        {
            flags += "excl ";
        }

        if (state.releasing)
        {
            flags += "ending";
        }

        return flags.Length > 0 ? flags.TrimEnd() : "-";
    }
}
