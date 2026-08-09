namespace FDK;

/// <summary>
/// What input events are stamped with. The game points this at the clock its audio runs on, so a hit and
/// the sound it triggers are timed against the same thing. A system timer stands in until it does.
/// </summary>
public static class InputClock
{
    private static readonly CTimer System = new(CTimer.EType.PerformanceCounter);

    public static IInputClock Current { get; set; } = System;
}
