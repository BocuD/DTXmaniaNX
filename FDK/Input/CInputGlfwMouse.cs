namespace FDK;

public class CInputGlfwMouse : IInputDevice
{
	private const int ButtonCount = 8;

	public EInputDeviceType eInputDeviceType => EInputDeviceType.Mouse;
	public string GUID => "";
	public int ID => 0;
	public List<STInputEvent> listInputEvent { get; } = new(32);
	public string strDeviceName => "GLFW Mouse";

	public void OnButtonEvent(int button, bool pressed)
	{
		if (button < 0 || button >= ButtonCount)
			return;

		lock (pendingEvents)
		{
			pendingEvents.Enqueue((button, pressed));
		}
	}

	public void tPolling(bool isWindowActive, bool useBufferedInput)
	{
		Array.Clear(bMousePushDown);
		Array.Clear(bMousePullUp);
		listInputEvent.Clear();

		lock (pendingEvents)
		{
			if (!isWindowActive)
			{
				pendingEvents.Clear();
				return;
			}

			while (pendingEvents.TryDequeue(out (int button, bool pressed) ev))
			{
				if (bMouseState[ev.button] == ev.pressed)
					continue;

				bMouseState[ev.button] = ev.pressed;
				(ev.pressed ? bMousePushDown : bMousePullUp)[ev.button] = true;

				listInputEvent.Add(new STInputEvent
				{
					nKey = ev.button,
					b押された = ev.pressed,
					b離された = !ev.pressed,
					nTimeStamp = InputClock.Current.nSystemTimeMs,
					nVelocity = CInputManager.nDefaultVelocity
				});
			}
		}
	}

	public bool bKeyPressed(int nButton) => IsButton(nButton) && bMousePushDown[nButton];
	public bool bKeyPressing(int nButton) => IsButton(nButton) && bMouseState[nButton];
	public bool bKeyReleased(int nButton) => IsButton(nButton) && bMousePullUp[nButton];
	public bool bKeyReleasing(int nButton) => IsButton(nButton) && !bMouseState[nButton];

	public void UpdateWindowHandle(IntPtr hWnd)
	{
	}

	public void Dispose()
	{
	}

	private static bool IsButton(int nButton) => nButton >= 0 && nButton < ButtonCount;

	private readonly bool[] bMousePushDown = new bool[ButtonCount];
	private readonly bool[] bMousePullUp = new bool[ButtonCount];
	private readonly bool[] bMouseState = new bool[ButtonCount];
	private readonly Queue<(int button, bool pressed)> pendingEvents = new();
}
