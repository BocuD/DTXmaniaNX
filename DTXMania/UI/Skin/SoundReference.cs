using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Skin;

/// <summary>
/// A sound a skin names and an element owns. The file is a <see cref="SkinResource"/> like any other; what
/// this adds is how it is played and the loaded sound itself, which lives no longer than whoever declared
/// it — a stage root loads its sounds when the stage builds and frees them when it tears down.
///
/// Loading is explicit rather than on first play: a sound played during a transition has to already be in
/// memory, and the first play is exactly when there is no time to read a file.
/// </summary>
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
    /// the previous copy is freed first.</summary>
    public void Load()
    {
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

    /// <summary>
    /// Frees the loaded sound. A one-shot that is still audible is handed over to finish first: the stage
    /// that owned it is going away, but the sound is often the transition out of it — a game-start jingle
    /// outlives the title screen by design. A loop is stopped outright, since it would never finish.
    /// </summary>
    public void Unload()
    {
        SweepFinished();

        if (loaded is not { } sound)
        {
            return;
        }

        loaded = null;

        if (!loop && sound.bIsPlaying)
        {
            finishing.Add(sound);
            return;
        }

        sound.tStop();
        sound.Dispose();
    }

    //handed-over sounds, freed once they fall silent. Nothing pumps this on a timer: it is swept whenever
    //sounds are loaded or freed, which is every stage change, so the most it ever holds is what was still
    //playing across the last one
    private static readonly List<CSystemSound> finishing = [];

    public static void SweepFinished()
    {
        for (int i = finishing.Count - 1; i >= 0; i--)
        {
            if (!finishing[i].bIsPlaying)
            {
                finishing[i].Dispose();
                finishing.RemoveAt(i);
            }
        }
    }

    public void Play(int volume = 100) => loaded?.tPlay(volume);

    public void Stop() => loaded?.tStop();

    public void RemoveMixer() => loaded?.tRemoveMixer();

    public void DrawInspector(string label)
    {
        if (!ImGui.CollapsingHeader(label))
        {
            return;
        }

        ImGui.PushID(label);

        ResourceEditor.Draw("File", ResourceType.Sound, sound, chosen =>
        {
            sound = chosen;
            Load();
        });

        if (ImGui.Checkbox("Loop", ref loop) || ImGui.Checkbox("Exclusive", ref exclusive))
        {
            //both are given to the sound when it is created, so they only take effect on a reload
            Load();
        }

        if (ImGui.Button("Play"))
        {
            Play();
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
            Stop();
        }

        ImGui.SameLine();
        ImGui.TextDisabled(IsLoaded ? IsPlaying ? "playing" : "loaded" : "not loaded");

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
