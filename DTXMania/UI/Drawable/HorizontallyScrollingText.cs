using System.Drawing;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Inspector;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// A <see cref="UIText"/> that clips itself to <see cref="maximumWidth"/> (logical px). When the
/// rendered text is wider than that and <see cref="scrollingEnabled"/> is set, it waits briefly,
/// scrolls the clip window until the right edge of the text reaches the limit, pauses, resets, and
/// repeats. Short text draws normally. Everything is driven by the timer sampled in Draw.
/// </summary>
public class HorizontallyScrollingText : UIText
{
    public bool scrollingEnabled;
    public float maximumWidth;
    public float scrollSpeed = 50.0f; //texture-space px per second

    public float pauseDuration = 2.0f; //seconds paused before scrolling and again before resetting

    [AddChildMenu]
    public static HorizontallyScrollingText Create()
    {
        return new();
    }
    
    private enum ScrollPhase
    {
        StartDelay, //parked at the start, waiting to begin scrolling
        Scrolling,  //moving toward the end
        EndPause    //parked at the end, waiting to reset
    }

    private ScrollPhase phase = ScrollPhase.StartDelay;
    private float phaseTimer;
    private float scrollOffset;
    private long lastDrawTime;

    //the clip width in texture (render-scale) pixels; comparable to texture.Width.
    private float maximumRenderWidth => maximumWidth * CDTXMania.renderScale;

    //true when the text doesn't fit and must be clipped/scrolled.
    private bool overflowing => maximumRenderWidth > 0f && texture.IsValid() && texture.Width > maximumRenderWidth;

    public HorizontallyScrollingText() : base()
    {
    }
    
    public HorizontallyScrollingText(string text, int size) : base(text, size)
    {
    }

    public override void Draw(Matrix4x4 parentMatrix)
    {
        long now = CDTXMania.Timer.nCurrentTime;
        float delta = MathF.Min((now - lastDrawTime) / 1000.0f, 0.1f); //clamp so a hitch can't jump the scroll
        lastDrawTime = now;

        if (scrollingEnabled && overflowing)
        {
            AdvanceScroll(delta);
        }
        else
        {
            ResetScroll();
        }

        base.Draw(parentMatrix);
    }

    private void ResetScroll()
    {
        phase = ScrollPhase.StartDelay;
        phaseTimer = 0f;
        scrollOffset = 0f;
    }

    private void AdvanceScroll(float delta)
    {
        //furthest we scroll: until the right edge of the text sits on the clip limit.
        float maxOffset = MathF.Max(texture.Width - maximumRenderWidth, 0f);
        phaseTimer += delta;

        switch (phase)
        {
            case ScrollPhase.StartDelay:
                if (phaseTimer >= pauseDuration)
                {
                    phase = ScrollPhase.Scrolling;
                    phaseTimer = 0f;
                }
                break;

            case ScrollPhase.Scrolling:
                scrollOffset += scrollSpeed * delta;
                if (scrollOffset >= maxOffset)
                {
                    scrollOffset = maxOffset;
                    phase = ScrollPhase.EndPause;
                    phaseTimer = 0f;
                }
                break;

            case ScrollPhase.EndPause:
                if (phaseTimer >= pauseDuration)
                {
                    scrollOffset = 0f;
                    phase = ScrollPhase.StartDelay;
                    phaseTimer = 0f;
                }
                break;
        }
    }

    protected override RectangleF GetTextureSourceRect()
    {
        if (!overflowing)
        {
            return base.GetTextureSourceRect();
        }

        return new RectangleF(scrollOffset, 0f, maximumRenderWidth, texture.Height);
    }

    protected override Vector2 GetTextureDrawSize()
    {
        if (!overflowing)
        {
            return base.GetTextureDrawSize();
        }

        return new Vector2(maximumRenderWidth, size.Y);
    }

    public override void DrawInspector()
    {
        base.DrawInspector();

        if (ImGui.CollapsingHeader("Horizontally Scrolling Text"))
        {
            ImGui.Checkbox("Scrolling enabled", ref scrollingEnabled);
            ImGui.InputFloat("Maximum Width", ref maximumWidth);
            ImGui.InputFloat("Scroll speed", ref scrollSpeed);
            ImGui.InputFloat("Pause duration", ref pauseDuration);
        }
    }
}
