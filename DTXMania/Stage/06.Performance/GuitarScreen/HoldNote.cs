using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Animation;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;

namespace DTXMania;

public class HoldNote : UIGroup
{
    [AddChildMenu]
    public static HoldNote Create()
    {
        return new HoldNote();
    }

    public TextureArray normal;
    public TextureArray wailing;
    
    public UIImage holdText;
    public UIImage staticHoldText;
    
    public float clipHeight;

    public HoldNote()
    {
        name = "Holdnote";
        
        BaseTexture[] normalTextures = new BaseTexture[5];
        BaseTexture[] wailingTextures = new BaseTexture[5];

        for (int index = 0; index < 5; index++)
        {
            normalTextures[index] = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Note\\Guitar\\hold0{index}.png"));
            normalTextures[index].blendMode = BlendMode.Additive;
            wailingTextures[index] = BaseTexture.LoadFromPath(CSkin.Path($"Graphics\\Note\\Guitar\\hold1{index}.png"));
            wailingTextures[index].blendMode = BlendMode.Additive;
        }
        
        normal = AddChild(new TextureArray(normalTextures));
        normal.name = "hold_normal";
        normal.anchor = new Vector2(0.5f, 0.0f);

        wailing = AddChild(new TextureArray(wailingTextures));
        wailing.name = "hold_wailing";
        wailing.anchor = new Vector2(0.5f, 0.0f);
        wailing.isVisible = false;
        
        holdText = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Guitar\hold.png"))));
        holdText.name = "hold_text";
        holdText.anchor = new Vector2(0.5f, 0.0f);
        holdText.scale = new Vector3(0.67f, 0.67f, 1.0f);
        holdText.position.Y = -3.0f;
        
        staticHoldText = AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\Note\Guitar\hold.png"))));
        staticHoldText.name = "hold_text_static";
        staticHoldText.anchor = new Vector2(0.5f, 0.0f);
        staticHoldText.scale = new Vector3(0.67f, 0.67f, 1.0f);
        staticHoldText.position.Y = -3.0f;

        animator = new Animator();
        AnimationClip? loaded = AnimationClipIO.LoadFromFile(CSkin.Path(@"Graphics\Note\Guitar\hold.json"));
        if (loaded != null)
        {
            animator.clips.Add(loaded);
        }
        animator.Play("hold");
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        normal.clipRect = new RectangleF(0, 0, normal.Texture.Width, clipHeight);
        wailing.clipRect = new RectangleF(0, 0, wailing.Texture.Width, clipHeight);
        
        normal.size.Y = clipHeight;
        wailing.size.Y = clipHeight;
        
        holdText.clipRect = new RectangleF(0, 0, holdText.Texture.Width, clipHeight);
        holdText.size.Y = clipHeight;
        
        base.Draw(parentMatrix);
    }

    public void Draw(Matrix4x4 parentMatrix, bool noteHit, bool isHittingLongNote)
    {
        staticHoldText.isVisible = !noteHit;

        normal.isVisible = isHittingLongNote;
        holdText.isVisible = isHittingLongNote;

        Draw(parentMatrix);
    }
}