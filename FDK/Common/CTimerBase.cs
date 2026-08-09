namespace FDK;

/// <summary>
/// A stopwatch over the clock a derived timer supplies. <see cref="tUpdate"/> samples that clock; every
/// read until the next one sees the same time, so a frame gets one consistent answer.
/// </summary>
public abstract class CTimerBase : IDisposable
{
	public const long nUnused = -1;

	public abstract long nSystemTimeMs { get; }

	public abstract void Dispose();

	/// <summary>Time since the last reset, not counting time spent paused.</summary>
	public long nCurrentTime
	{
		get => nSampledMs - nResetAtMs;
		set => nResetAtMs = nSampledMs - value;
	}

	public long nResetAtMs { get; protected set; }

	public void tUpdate()
	{
		if (nPauseDepth == 0)
			nSampledMs = nSystemTimeMs;
	}

	public void tReset()
	{
		nSampledMs = nSystemTimeMs;
		nResetAtMs = nSampledMs;
		nPauseDepth = 0;
	}

	/// <summary>Nests: a second pause needs a second resume.</summary>
	public void tPause() => nPauseDepth++;

	public void tResume()
	{
		if (nPauseDepth == 0)
			return;

		nPauseDepth--;

		if (nPauseDepth == 0)
			nResetAtMs += nSystemTimeMs - nSampledMs;
	}

	private long nSampledMs;
	private int nPauseDepth;
}
