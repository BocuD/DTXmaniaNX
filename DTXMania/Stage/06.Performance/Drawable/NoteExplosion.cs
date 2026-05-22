using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania.Drawable;

public class NoteExplosion : UIGroup
{
    [AddChildMenu]
    public static NoteExplosion Create()
    {
        return new NoteExplosion();
    }
    
    public NoteExplosion()
    {
        name = $"NoteExplosion";

        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Explosion\hit.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }

        isVisible = false;
    }
}