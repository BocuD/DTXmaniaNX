using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania.Drawable;

public class WailingEffect : UIGroup
{
    [AddChildMenu]
    public static WailingEffect Create() => new();
    
    public WailingEffect()
    {
        //a gameplay element is runtime-only: it never appears in a saved layout
        dontSerialize = true;
        name = "WailingEffect";

        BaseTexture[] wailingTextures = new BaseTexture[21];
        
        for (int i = 0; i < wailingTextures.Length; i++)
        {
            wailingTextures[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\wailing{(i):00}.png"));
        }
        
        array = AddChild(new TextureArray(wailingTextures));
        array.name = "wailingAnimation";
        array.scale = new Vector3(1, 0.87f, 1);

        //the frames run themselves; the clip drops the rate once the attack is over and fades the effect
        //out. Frame 9 is where the shimmer starts, so the first nine play once and the rest loop
        array.frameRate = 60.0f;
        array.loopStart = 9;

        array.color.Alpha = 0;

        //loaded outright rather than referenced: a gameplay effect is never part of a saved layout
        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Note\Guitar\wailing.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }
    }

    private readonly TextureArray array;

    public void Play()
    {
        array.textureIndex = 0;
        animator.Play("wailing");
    }
}