using Hexa.NET.ImGui;

namespace DTXMania.UI.Drawable;

/// <summary>
/// How a scrolling list travels towards the position it has been asked to go to. Speed is a function of
/// how far is left, which is what both of the game's list feels reduce to: the song list eases to a stop
/// (speed proportional to the distance left), the settings list runs at a constant speed and lands, and
/// speeds up while a held key builds a backlog.
///
/// Kept separate from the list so the feel is data, the way <see cref="UIItemCurve"/> is for shape: every
/// parameter is <c>[Themable]</c>, it serializes into the layout, and it can be reasoned about on its own.
/// Everything here is measured in items per second, so a list keeps its feel whatever its spacing is.
/// </summary>
public sealed class UIScrollMotion
{
    //speed gained per item still to travel, which is what makes the list ease to a stop rather than
    //arrive at full speed
    [Themable] public float rate = 10.0f;

    //floor on speed; above zero the list covers the last stretch at a constant speed and lands on the
    //item instead of creeping up on it
    [Themable] public float minSpeed;

    //ceiling on speed; zero for none
    [Themable] public float maxSpeed;

    //how far ahead of what is on screen a held key may queue; zero for no limit, and with a limit this
    //and the speed together decide how fast a held key scrolls
    [Themable] public float queueLimit = 2.0f;

    public UIScrollMotion()
    {
    }

    public UIScrollMotion(float rate, float minSpeed = 0.0f, float maxSpeed = 0.0f, float queueLimit = 0.0f)
    {
        this.rate = rate;
        this.minSpeed = minSpeed;
        this.maxSpeed = maxSpeed;
        this.queueLimit = queueLimit;
    }

    /// <summary>Speed, in items per second, with <paramref name="itemsRemaining"/> left to travel.</summary>
    public float SpeedAt(float itemsRemaining)
    {
        float speed = Math.Max(rate * Math.Abs(itemsRemaining), minSpeed);
        return maxSpeed > 0.0f ? Math.Min(speed, maxSpeed) : speed;
    }

    /// <summary>
    /// The speed a held key settles at, or zero when nothing bounds it: an unlimited queue with no
    /// ceiling scrolls as fast as the key repeats.
    /// </summary>
    public float TopSpeed => queueLimit > 0.0f ? SpeedAt(queueLimit) : maxSpeed;

    /// <summary>How far to move, in items, with <paramref name="remaining"/> items left. Never overshoots.</summary>
    public float Step(float remaining, float elapsedSeconds)
    {
        float distance = Math.Abs(remaining);

        if (distance <= 0.0f || elapsedSeconds <= 0.0f)
        {
            return 0.0f;
        }

        //the proportional part is integrated exactly rather than as rate * elapsed of what is left, so a
        //move takes the same time at any frame rate and no step size can overshoot
        float step = distance * (1.0f - MathF.Exp(-rate * elapsedSeconds));
        step = Math.Max(step, minSpeed * elapsedSeconds);

        if (maxSpeed > 0.0f)
        {
            step = Math.Min(step, maxSpeed * elapsedSeconds);
        }

        step = Math.Min(step, distance);
        return remaining < 0.0f ? -step : step;
    }

    /// <summary>Clamps queued travel, in items, to what a held key is allowed to run ahead by.</summary>
    public float ClampQueue(float queued)
        => queueLimit > 0.0f ? Math.Clamp(queued, -queueLimit, queueLimit) : queued;

    /// <summary>Drawn by every list that owns a motion, so they all expose the same controls.</summary>
    public void DrawInspector()
    {
        ImGui.InputFloat("Rate", ref rate);
        ImGui.InputFloat("Min Speed", ref minSpeed);
        ImGui.InputFloat("Max Speed", ref maxSpeed);
        ImGui.InputFloat("Queue Limit", ref queueLimit);
        ImGui.LabelText("Top Speed", TopSpeed > 0.0f ? $"{TopSpeed:0.#} items/sec" : "as fast as the key repeats");
    }
}
