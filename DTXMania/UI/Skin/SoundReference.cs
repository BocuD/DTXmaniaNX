using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

[SkinSerialize]
public sealed class SoundReference
{
    [Themable] public SkinResource sound;

    [Themable] public bool loop;

    //stops whatever exclusive sound was playing before it, so background music replaces rather than layers
    [Themable] public bool exclusive;
    [JsonIgnore] private CSystemSound? loaded;

    public SoundReference()
    {
    }

    public SoundReference(SkinResource sound, bool loop = false, bool exclusive = false)
    {
        this.sound = sound;
        this.loop = loop;
        this.exclusive = exclusive;
    }

    [JsonIgnore] public bool IsPlaying => loaded?.bIsPlaying ?? false;

    [JsonIgnore] public bool IsLoaded => loaded is { loadSucceeded: true };

    /// <summary>Reads the file now, so it is ready before anything asks to play it. Safe to call again;
    /// the previous copy is freed first</summary>
    public void Load()
    {
        //pointing this slot at something else stops what it was playing: only an owner going away lets a
        //one-shot finish, and the old sound is not what the slot means any more
        Unload();

        if (sound.IsEmpty)
        {
            return;
        }

        string path = sound.Resolve(ResourceType.Sound);

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Trace.TraceError($"Sound {sound} could not be found.");
            return;
        }

        CSystemSound created = CSystemSound.FromPath(path, loop, exclusive);

        try
        {
            created.tRead();
            loaded = created;
        }
        catch (Exception e)
        {
            Trace.TraceError($"Failed to load sound {sound}: {e.Message}");
            created.Dispose();
        }
    }

    /// <summary>Stops the loaded sound and frees it.</summary>
    public void Unload()
    {
        loaded?.tStop();
        loaded?.Dispose();
        loaded = null;
    }

    //frees the sound, but lets a one-shot that is still audible finish first. The mixer owns the channels,
    //so letting them run on is its business rather than a list kept here
    public void ReleaseWhenFinished()
    {
        loaded?.ReleaseWhenFinished();
        loaded = null;
    }

    public void Play(int volume = 100) => loaded?.tPlay(volume);

    public void Stop() => loaded?.tStop();

    public void RemoveMixer() => loaded?.tRemoveMixer();

    /// <summary>One line of what this sound is, for whoever lists it.</summary>
    [JsonIgnore] public string Summary => sound.ToString();

    //a missing file is called out by the resource editor itself, so this only says where playback stands
    [JsonIgnore] private string State => !IsLoaded
        ? sound.IsEmpty ? "no file" : "not loaded"
        : IsPlaying ? "playing" : "ready";

    /// <summary>Draws the fields only. The caller owns the heading, so a stage's sounds can be listed
    /// under one of its own rather than each looking like a section of the inspector.</summary>
    public void DrawInspector(string label)
    {
        ImGui.PushID(label);

        ResourceEditor.Draw("File", ResourceType.Sound, sound, chosen =>
        {
            sound = chosen;
            Load();
        });

        //both are handed to the sound when it is created, so a change only takes hold on a reload
        if (ImGui.Checkbox("Loop", ref loop))
        {
            Load();
        }

        ImGui.SameLine();
        if (ImGui.Checkbox("Exclusive", ref exclusive))
        {
            Load();
        }

        ImGui.BeginDisabled(!IsLoaded);
        if (ImGui.Button("Play"))
        {
            Play();
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
            Stop();
        }

        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.TextDisabled(State);

        ImGui.PopID();
    }

    //compared by value so compact serialization can tell an untouched sound from an edited one, the way
    //every other themable member is compared
    public override bool Equals(object? obj)
    {
        return obj is SoundReference other
               && sound == other.sound
               && loop == other.loop
               && exclusive == other.exclusive;
    }

    public override int GetHashCode() => HashCode.Combine(sound, loop, exclusive);
}
