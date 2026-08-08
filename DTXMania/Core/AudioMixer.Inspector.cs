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

        ImGui.TextDisabled("Levels are not saved — Config > Audio > Mixer Volumes is where they persist.");

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

        int volume = Device.GetGroupVolume(group);

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

        ImGui.Text($"Mixer   clips {clips.Count}   voices {channels} (peak {PeakVoiceCount})   playing {sounding}");

        ImGui.Text($"FDK     streams {FDK.CSoundManager.nStreams}   in mix {FDK.CSoundManager.nMixing}");

        ImGui.TextDisabled("In mix counts channels attached to the output, playing or not, and only the "
                           + "ones FDK made.");

        //normal while a loader still has clips it has not published
        int unaccounted = UnaccountedClips;

        if (unaccounted != 0)
        {
            ImGui.TextColored(Busy, $"Live clips {LiveClips}, {unaccounted} not in this list " +
                                    "(loading, or leaked)");
        }

        ImGui.TextDisabled($"{Device.TypeName} on " +
                           $"{(Device.CurrentOutput.Length > 0 ? Device.CurrentOutput : "an unnamed device")}");

        ImGui.TextDisabled(CSystemSound.rLastPlayedExclusiveSystemSound is { } exclusive
            ? $"exclusive: {exclusive.strFilename}"
            : "exclusive: none");
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
        ImGui.TextDisabled(clip.audio?.VoiceKind ?? "-");
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
