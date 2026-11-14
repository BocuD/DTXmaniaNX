using SlimDXKey = SlimDX.DirectInput.Key;

namespace FDK;

public class CInputGlfwKeyboard : IInputKeyboard
{
	public EInputDeviceType eInputDeviceType => EInputDeviceType.Keyboard;
	public string GUID => "";
	public int ID => 0;
	public List<STInputEvent> listInputEvent { get; } = new(32);
	public string strDeviceName => "GLFW Keyboard";

	public bool preventKeyboardInput { get; set; }

	public void OnKeyEvent(SlimDXKey key, bool pressed)
	{
		if (key == SlimDXKey.Unknown)
			return;

		lock (pendingEvents)
		{
			pendingEvents.Enqueue((key, pressed));
		}
	}

	public void tPolling(bool isWindowActive, bool useBufferedInput)
	{
		Array.Clear(bKeyPushDown);
		Array.Clear(bKeyPullUp);
		listInputEvent.Clear();

		lock (pendingEvents)
		{
			if (!isWindowActive)
			{
				pendingEvents.Clear();
				return;
			}

			while (pendingEvents.TryDequeue(out (SlimDXKey key, bool pressed) ev))
			{
				int nKey = (int)ev.key;
				if (bKeyState[nKey] == ev.pressed)
					continue;

				bKeyState[nKey] = ev.pressed;
				(ev.pressed ? bKeyPushDown : bKeyPullUp)[nKey] = true;

				listInputEvent.Add(new STInputEvent
				{
					nKey = nKey,
					b押された = ev.pressed,
					b離された = !ev.pressed,
					nTimeStamp = InputClock.Current.nSystemTimeMs,
					nVelocity = CInputManager.nDefaultVelocity
				});
			}
		}
	}

	public bool bKeyPressed(int nKey) => !preventKeyboardInput && bKeyPushDown[nKey];
	public bool bKeyPressing(int nKey) => !preventKeyboardInput && bKeyState[nKey];
	public bool bKeyReleased(int nKey) => !preventKeyboardInput && bKeyPullUp[nKey];
	public bool bKeyReleasing(int nKey) => !preventKeyboardInput && !bKeyState[nKey];

	public void UpdateWindowHandle(IntPtr hWnd)
	{
	}

	public void Dispose()
	{
	}

	private readonly bool[] bKeyPushDown = new bool[256];
	private readonly bool[] bKeyPullUp = new bool[256];
	private readonly bool[] bKeyState = new bool[256];
	private readonly Queue<(SlimDXKey key, bool pressed)> pendingEvents = new();
}
