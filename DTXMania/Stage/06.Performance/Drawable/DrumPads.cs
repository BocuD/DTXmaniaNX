using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Drawable;
using DTXMania.UI.Inspector;
using DTXMania.UI.Skin;
using Hexa.NET.ImGui;

namespace DTXMania;

public class DrumPad
{
    public ELane lane;
    public SkinResource image;
    //x from the lane's centre to the image's centre, y to the image's top from the element
    public Vector2 offset;
    //what the art's colours are multiplied by at full brightness. Above 1 is drawn a second time, additively
    public Color4 litColour = Color4.White;
}

//the pads under the lanes. They light up and bounce when hit. The list order is the draw order
public class DrumPads : UIDrawable
{
    [AddChildMenu("Game/Performance/Drum Pads")]
    public static UIDrawable Create() => new DrumPads();

    [Themable] [SkinSerialize] public List<DrumPad> pads = [];

    //set by the playfield
    public LaneLayout lanes = new();

    //the lane of each pad number the stage hits
    private static readonly ELane[] PadLanes =
        [ELane.LC, ELane.HH, ELane.SD, ELane.BD, ELane.HT, ELane.LT, ELane.FT, ELane.CY, ELane.RD, ELane.LP];

    private struct PadState
    {
        public int brightness;
        public int bounce;
        public int bounceSpeed;
    }

    private PadState[] states = [];
    private BaseTexture?[] art = [];
    private SkinResource[] loadedImages = [];
    private long brightnessTimer = -1;
    private long bounceTimer = -1;

    public DrumPads()
    {
        name = "Pads";
    }

    //pad: LC HH SD BD HT LT FT CY RD LP
    public void Hit(int pad)
    {
        ELane lane = PadLanes[pad];
        Prepare();
        for (int index = 0; index < pads.Count; index++)
        {
            if (pads[index].lane == lane)
            {
                Light(index);
            }
        }
    }

    private void Light(int index)
    {
        states[index].brightness = 6;
        states[index].bounceSpeed = 2;
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        Prepare();
        Animate();

        UpdateLocalTransformMatrix();
        Matrix4x4 origin = localTransformMatrix * parentMatrix;
        for (int index = 0; index < pads.Count; index++)
        {
            DrumPad pad = pads[index];
            BaseTexture? texture = art[index];
            if (texture == null || !lanes.Has(pad.lane))
            {
                continue;
            }

            int x = (int)(lanes.Centre(pad.lane) + pad.offset.X - texture.Width / 2f);
            int y = (int)pad.offset.Y + states[index].bounce;
            DrawPad(origin, texture, pad.litColour, states[index].brightness, x, y);
        }
    }

    private static void DrawPad(Matrix4x4 origin, BaseTexture texture, Color4 litColour, int brightness, int x, int y)
    {
        RectangleF source = new(0, 0, texture.Width, texture.Height);
        Color4 tint = UITint.Current;
        float lit = Math.Min(brightness * 50 / 255f, 1f);
        if (lit <= 0)
        {
            texture.tDraw2D(origin, x, y, source, tint);
            return;
        }

        float r = 1 + (litColour.Red - 1) * lit;
        float g = 1 + (litColour.Green - 1) * lit;
        float b = 1 + (litColour.Blue - 1) * lit;
        texture.tDraw2D(origin, x, y, source, tint * new Color4(Math.Min(r, 1), Math.Min(g, 1), Math.Min(b, 1)));

        texture.blendMode = BlendMode.Additive;
        texture.tDraw2D(origin, x, y, source, tint * new Color4(Math.Max(r - 1, 0), Math.Max(g - 1, 0), Math.Max(b - 1, 0)));
        texture.blendMode = BlendMode.Alpha;
    }

    //one state and texture per pad, reloaded when a pad's image changes
    private void Prepare()
    {
        if (states.Length != pads.Count)
        {
            DisposeArt();
            states = new PadState[pads.Count];
            art = new BaseTexture?[pads.Count];
            loadedImages = new SkinResource[pads.Count];
        }

        for (int index = 0; index < pads.Count; index++)
        {
            SkinResource image = pads[index].image;
            if (art[index] != null && image == loadedImages[index])
            {
                continue;
            }

            art[index]?.Dispose();
            art[index] = image.IsEmpty ? null : BaseTexture.LoadFromPath(image.Resolve(ResourceType.Image));
            loadedImages[index] = image;
        }
    }

    //brightness drops a step every 18ms, the bounce moves every 8ms
    private void Animate()
    {
        long now = CDTXMania.Timer.nCurrentTime;
        if (brightnessTimer < 0 || brightnessTimer > now)
        {
            brightnessTimer = now;
        }

        if (bounceTimer < 0 || bounceTimer > now)
        {
            bounceTimer = now;
        }

        for (; now - brightnessTimer >= 18; brightnessTimer += 18)
        {
            for (int index = 0; index < states.Length; index++)
            {
                states[index].brightness = Math.Max(states[index].brightness - 1, 0);
            }
        }

        for (; now - bounceTimer >= 6; bounceTimer += 8)
        {
            for (int index = 0; index < states.Length; index++)
            {
                ref PadState state = ref states[index];
                state.bounce += state.bounceSpeed;
                if (state.bounce > 11)
                {
                    state.bounce = 11;
                    state.bounceSpeed = -1;
                }
                else if (state.bounce < 0)
                {
                    state.bounce = 0;
                    state.bounceSpeed = 0;
                }
            }
        }
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (!ImGui.CollapsingHeader("Pads"))
        {
            return;
        }

        for (int index = 0; index < pads.Count; index++)
        {
            DrumPad pad = pads[index];
            if (!ImGui.TreeNode($"{index}: {pad.lane}###pad{index}"))
            {
                continue;
            }

            Inspector.Inspect("Lane", ref pad.lane);
            ResourceEditor.Draw("Image", ResourceType.Image, pad.image, chosen => pad.image = chosen);
            Inspector.Inspect("Offset", ref pad.offset);
            Inspector.Inspect("Lit Colour", ref pad.litColour);
            if (ImGui.Button("Hit"))
            {
                Light(index);
            }

            ImGui.TreePop();
        }
    }

    private void DisposeArt()
    {
        foreach (BaseTexture? texture in art)
        {
            texture?.Dispose();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        DisposeArt();
        art = [];
    }
}
