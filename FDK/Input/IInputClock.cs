namespace FDK;

/// <summary>The clock input events are stamped with.</summary>
public interface IInputClock
{
	long nSystemTimeMs { get; }

	/// <summary>A timestamp from the input device's own clock, translated onto this one.</summary>
	long nSystemTimeMsFor(long deviceTimestamp);
}
