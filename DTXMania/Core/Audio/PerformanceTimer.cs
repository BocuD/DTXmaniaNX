using FDK;

namespace DTXMania.Core.Audio;

public sealed class PerformanceTimer : CTimerBase, IInputClock
{
    public override long nSystemTimeMs => AudioMixer.Device.ElapsedMs;

    public long nSystemTimeMsFor(long deviceTimestamp)
        => AudioMixer.Device.ElapsedMsFor(deviceTimestamp);

    public override void Dispose()
    {
    }
}
