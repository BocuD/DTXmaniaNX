using System.Reflection;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;
using Newtonsoft.Json;

namespace DTXMania.UI.Drawable;

/// <summary>
/// The root of one stage's tree, and where that stage keeps what belongs to it rather than to the game:
/// its own sounds, and the clip to play as it opens. A subclass only has to declare a
/// <see cref="SoundReference"/> field — loading and freeing follow from that.
/// </summary>
public class StageRoot : UIGroup
{
    //a clip in this root's own animator, played once as the stage opens; empty for none
    [Themable] public string openClip = string.Empty;

    //most stages have background music and the ones that don't leave it empty, so it lives here rather
    //than being redeclared per stage. A subclass names its file in its constructor
    public SoundReference bgm = new();

    //this root's own SoundReference fields, found once the tree is built. Deserialization replaces the
    //instances the constructor made, so this cannot be collected any earlier
    [JsonIgnore] private SoundReference[]? sounds;

    public StageRoot() : base("StageRoot")
    {
    }

    public StageRoot(string name) : base(name)
    {
    }

    /// <summary>Reads this stage's sounds into memory. Called as the stage's tree is built, so a sound
    /// played during the transition into the stage is already there when it is asked for.</summary>
    public void LoadSounds()
    {
        sounds = DeclaredSounds();

        foreach (SoundReference sound in sounds)
        {
            sound.Load();
        }
    }

    /// <summary>Runs once the stage's tree is built, before it is first drawn.</summary>
    public virtual void OnStageOpened()
    {
        if (openClip.Length > 0)
        {
            animator?.Play(openClip);
        }
    }

    public override void Dispose()
    {
        //only what was loaded, so a root that never opened does not touch the sound device on the way out
        foreach (SoundReference sound in sounds ?? [])
        {
            sound.Unload();
        }

        sounds = null;

        base.Dispose();
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Stage"))
        {
            return;
        }

        ImGui.InputText("Open Clip", ref openClip, 128);

        foreach (FieldInfo field in SoundFields(GetType()))
        {
            if (field.GetValue(this) is SoundReference sound)
            {
                sound.DrawInspector(field.Name);
            }
        }
    }

    private SoundReference[] DeclaredSounds()
    {
        List<SoundReference> found = [];

        foreach (FieldInfo field in SoundFields(GetType()))
        {
            if (field.GetValue(this) is SoundReference sound)
            {
                found.Add(sound);
            }
        }

        return found.ToArray();
    }

    private static IEnumerable<FieldInfo> SoundFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => field.FieldType == typeof(SoundReference));
    }
}
