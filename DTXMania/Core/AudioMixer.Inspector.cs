using System.Numerics;
using DTXMania.Core.Audio;
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

        if (ImGui.Button("Stop all playback"))
        {
            foreach (MixerClip clip in clips)
            {
                Stop(clip);
            }
        }

        foreach (AudioGroup group in Enum.GetValues<AudioGroup>())
        {
            DrawGroup(group);
        }

        ImGui.End();
    }

    private static void DrawGroup(AudioGroup group)
    {
        int count = 0;
        int channels = 0;
        int sounding = 0;

        foreach (MixerClip clip in clips)
        {
            if (clip.group != group)
            {
                continue;
            }

            count++;
            channels += clip.voices.Count;
            sounding += Sounding(clip);
        }

        bool open = ImGui.CollapsingHeader($"{group}   {count} clips, {channels} voices, {sounding} playing###{group}");

        int volume = GetGroupVolume(group);

        ImGui.PushID((int)group);
        ImGui.SetNextItemWidth(200.0f);

        if (ImGui.SliderInt("Level", ref volume, 0, 100))
        {
            SetGroupVolume(group, volume);
        }

        ImGui.PopID();

        if (open)
        {
            DrawClips(group, count);
        }
    }

    private static void DrawTotals()
    {
        int channels = 0;
        int sounding = 0;

        foreach (MixerClip clip in clips)
        {
            channels += clip.voices.Count;
            sounding += Sounding(clip);
        }

        AudioDeviceStatus audio = Device.Status;

        ImGui.Text($"Mixer   clips {clips.Count}   voices {channels} (peak {PeakVoiceCount})   playing {sounding}");

        ImGui.Text($"Output  streams {audio.Streams}   in mix {audio.MixedChannels}");

        //normal while a loader still has clips it has not published
        int unaccounted = UnaccountedClips;

        if (unaccounted != 0)
        {
            ImGui.TextColored(Busy, $"Live clips {LiveClips}, {unaccounted} not in this list " +
                                    "(loading, or leaked)");
        }

        ImGui.TextDisabled($"{audio.Backend}{(audio.Legacy ? " (legacy FDK)" : "")}"
                           + $"{(audio.Mode.Length > 0 ? $" {audio.Mode}" : "")} on " +
                           $"{(audio.Output.Length > 0 ? audio.Output : "an unnamed device")}"
                           + $"{(audio.SampleRate > 0 ? $", {audio.SampleRate}Hz" : "")}");

        DrawBuffer(audio, Device.Latency);

        DrawLatency(audio, Device.Latency);
        DrawDeviceSwap();

        ImGui.TextDisabled(CSystemSound.rLastPlayedExclusiveSystemSound is { } exclusive
            ? $"exclusive: {exclusive.strFilename}"
            : "exclusive: none");
    }

    /// <summary>
    /// The buffer and the wait separately, since a driver whose path reaches past its own buffer reports
    /// a larger latency and the buffer still has to match its control panel.
    /// </summary>
    private static void DrawBuffer(AudioDeviceStatus audio, AudioLatency latency)
    {
        if (audio.BufferMs < 0)
        {
            ImGui.TextDisabled("buffer   not reported");
            return;
        }

        string buffer = audio.BufferFrames > 0
            ? $"{audio.BufferFrames} {audio.FrameUnit} ({audio.BufferLatencyMs:0.0}ms)"
            : $"{audio.BufferLatencyMs:0.0}ms";

        string wait = latency.IsKnown
            ? $"{latency.Ms:0.0}ms"
            : "not reported";

        ImGui.TextDisabled($"buffer   {buffer}   latency {wait}");

        //ASIO hands over one buffer per callback, so saying the fill is the same size says nothing
        if (audio.PeriodMs > 0.0 && audio.PeriodFrames != audio.BufferFrames)
        {
            ImGui.TextDisabled($"fill     {audio.PeriodFrames} {audio.FrameUnit} ({audio.PeriodMs:0.0}ms)");
        }
    }

    /// <summary>
    /// How long a hit takes to be heard, of the parts that can be known. Chart audio does not suffer
    /// this: chips are scheduled against the output's own clock, so the buffer is already accounted for.
    /// </summary>
    private static void DrawLatency(AudioDeviceStatus audio, AudioLatency latency)
    {
        //a hit is noticed and played on the frame it arrives, so the frame is part of the wait. The null
        //check is for shutdown, where the counter is gone before the window stops drawing
        double frame = CDTXMania.FPS is { nCurrentFPS: > 0 } fps ? 1000.0 / fps.nCurrentFPS : 0.0;

        if (!latency.IsKnown)
        {
            ImGui.Text($"Hit to sound   frame {frame:0.0}ms, output not reported");
            return;
        }

        ImGui.Text($"Hit to sound   {frame + latency.Ms:0.0}ms"
                   + $"   (frame {frame:0.0}ms)");
    }

    /// <summary>
    /// Swaps between FDK's sound device and this layer's own, on the backend config already names. For
    /// comparing the two by ear; goes when FDK's audio does.
    /// </summary>
    private static void DrawDeviceSwap()
    {
        //a rebuild frees and reloads every sound the chart is holding, which mid-song is a hang
        bool duringSong = CDTXMania.StageManager?.rCurrentStage?.eStageID == CStage.EStage.Performance_6;
        bool fdk = CDTXMania.ConfigIni.bUseFDKAudio;

        ImGui.BeginDisabled(duringSong);

        if (ImGui.Checkbox("Play through FDK's device", ref fdk))
        {
            CDTXMania.ConfigIni.bUseFDKAudio = fdk;
            RetryOutput();
            Reinitialize(AudioDeviceOptions.FromConfig(CDTXMania.ConfigIni));
            CDTXMania.app.UpdateWindowTitle();
        }

        ImGui.EndDisabled();

        if (duringSong)
        {
            ImGui.TextDisabled("Unavailable during a song");
        }
    }

    private static void DrawClips(AudioGroup group, int count)
    {
        if (count == 0)
        {
            ImGui.TextDisabled("Nothing loaded.");
            return;
        }

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                                                              | ImGuiTableFlags.SizingStretchProp
                                                              | ImGuiTableFlags.ScrollY;

        //capped so one group cannot push the others off the window
        float height = Math.Min(count + 1, 12) * ImGui.GetTextLineHeightWithSpacing();

        if (!ImGui.BeginTable($"mixerClips{group}", 5, flags, new Vector2(0.0f, height)))
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

        //insertion order, so a row stays where it was as counts change
        foreach (MixerClip clip in clips)
        {
            if (clip.group == group)
            {
                DrawClipRow(clip);
            }
        }

        ImGui.EndTable();
    }

    private static void DrawClipRow(MixerClip clip)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.Text(clip.name);

        //full means the next play grows the pool, which is when a decode happens
        ImGui.TableNextColumn();
        int sounding = Sounding(clip);
        bool saturated = sounding > 0 && sounding == clip.voices.Count;

        if (saturated)
        {
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Busy);
        }

        float used = clip.voices.Count == 0 ? 0.0f : sounding / (float)clip.voices.Count;
        ImGui.ProgressBar(used, new Vector2(-1.0f, 0.0f), $"{sounding}/{clip.voices.Count}");

        if (saturated)
        {
            ImGui.PopStyleColor();
        }

        ImGui.TableNextColumn();
        ImGui.Text(clip.plays.ToString());

        ImGui.TableNextColumn();
        ImGui.TextDisabled(Flags(clip));

        ImGui.TableNextColumn();
        ImGui.TextDisabled(VoiceKind(clip));
    }

    private static int Sounding(MixerClip clip)
    {
        int count = 0;

        foreach (Voice voice in clip.voices)
        {
            if (voice.sound.IsPlaying)
            {
                count++;
            }
        }

        return count;
    }

    private static string Flags(MixerClip clip)
    {
        string flags = string.Empty;

        if (clip.loop)
        {
            flags += "loop ";
        }

        if (clip.releasing)
        {
            flags += "ending";
        }

        return flags.Length > 0 ? flags.TrimEnd() : "-";
    }
}
