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
        name = "WailingEffect";

        BaseTexture[] wailingTextures = new BaseTexture[21];
        
        for (int i = 0; i < wailingTextures.Length; i++)
        {
            wailingTextures[i] = BaseTexture.LoadFromPath(CSkin.Path($@"Graphics\Note\Guitar\wailing{(i):00}.png"));
        }
        
        var array = AddChild(new TextureArray(wailingTextures));
        array.name = "wailingAnimation";
        array.scale = new Vector3(1, 0.87f, 1);

        array.color.Alpha = 0;
        
        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Note\Guitar\wailing.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }
    }

    public void Play()
    {
        animator.Play("wailing");
    }
}