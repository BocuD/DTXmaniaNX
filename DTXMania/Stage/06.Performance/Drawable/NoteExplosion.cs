using System.Numerics;
using System.Windows.Forms;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.Drawable;

public class NoteExplosion : UIGroup
{
    [AddChildMenu]
    public static NoteExplosion Create()
    {
        return new NoteExplosion(new Color4(1.0f, 1.0f, 0.0f));
    }

    public NoteExplosion(Color4 color, bool hasCircle = true)
    {
        name = $"NoteExplosion";

        var explode = new BaseTexture[8];
        for (int i = 0; i < explode.Length; i++)
        {
            explode[i] = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Note\\Explosion\\Default_1{i}.png"));
            explode[i].blendMode = BlendMode.Additive;
        }

        var textureArray = AddChild(new TextureArray(explode));
        textureArray.anchor = new Vector2(0.5f, 0.5f);
        textureArray.color = color;
        textureArray.name = "attack0";

        var explode2 = new BaseTexture[8];
        for (int i = 0; i < explode2.Length; i++)
        {
            explode2[i] = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Note\\Explosion\\Default_0{i}.png"));
            explode2[i].blendMode = BlendMode.Additive;
        }

        var textureArray2 = AddChild(new TextureArray(explode2));
        textureArray2.anchor = new Vector2(0.5f, 0.5f);
        textureArray2.name = "attack1";

        var circleTex = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Explosion\circle01.png"));
        circleTex.blendMode = BlendMode.Additive;
        var circle = AddChild(new UIImage(circleTex));
        circle.anchor = new Vector2(0.5f, 0.5f);
        circle.color = color;
        circle.name = "circle";
        circle.isVisible = hasCircle;

        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Note\Explosion\explode.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }

        isVisible = false;
    }
    
    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Explosion"))
        {
            if (ImGui.Button("Play")) Play();
        }
    }

    private CChip? lastChip = null;
    
    public void Play(CChip? chip = null)
    {
        isVisible = true;

        if (chip != null)
        {
            if (chip != lastChip)
            {
                lastChip = chip;
            }
            else
            {
                return;
            }
        }
        
        //fast forward one frame
        animator.time = 1 / 60.0f;
        animator.Play("explode");
    }
}