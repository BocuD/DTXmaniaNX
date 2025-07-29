using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX.DirectInput;

namespace FDK;

public class CInputManager : IDisposable // CInput管理
{ 
	//input velocity used by non analog devices
	public static int nDefaultVelocity = 110;
	
	public List<IInputDevice> listInputDevices // list入力デバイス
	{
		get;
		private set;
	}

	public CInputKeyboard Keyboard
	{
		get
		{
			if (_Keyboard != null)
			{
				return _Keyboard;
			}

			foreach (IInputDevice device in listInputDevices)
			{
				if (device.eInputDeviceType == EInputDeviceType.Keyboard)
				{
					_Keyboard = (CInputKeyboard)device;
					return _Keyboard;
				}
			}

			return null;
		}
	}

	public CInputMouse Mouse
	{
		get
		{
			if (_Mouse != null)
			{
				return _Mouse;
			}

			foreach (IInputDevice device in listInputDevices)
			{
				if (device.eInputDeviceType == EInputDeviceType.Mouse)
				{
					_Mouse = (CInputMouse)device;
					return _Mouse;
				}
			}

			return null;
		}
	}
	
	public CInputManager(IntPtr hWnd, bool bUseMidiIn = true)
	{
		InitializeInputManager(hWnd, bUseMidiIn);
	}

	private void InitializeInputManager(IntPtr hWnd, bool bUseMidiIn)
	{
		directInput = new DirectInput();
		listInputDevices = new List<IInputDevice>(10);
		
		//enumerate keyboard and mouse devices
		CInputKeyboard cinputkeyboard = null;
		CInputMouse cinputmouse = null;
		try
		{
			cinputkeyboard = new CInputKeyboard(hWnd, directInput);
			cinputmouse = new CInputMouse(hWnd, directInput);
		}
		catch (Exception e)
		{
			Trace.TraceError("CInputManager: Failed to initialize keyboard/mouse input devices: {0}", e.Message);
		}
		if (cinputkeyboard != null)
		{
			listInputDevices.Add(cinputkeyboard);
		}
		if (cinputmouse != null)
		{
			listInputDevices.Add(cinputmouse);
		}
		
		//enumerate joystick devices
		foreach (DeviceInstance instance in directInput.GetDevices(DeviceClass.GameControl,
			         DeviceEnumerationFlags.AttachedOnly))
		{
			listInputDevices.Add(new CInputJoystick(hWnd, instance, directInput));
		}
		
		//enumerate MIDI devices
		if (bUseMidiIn)
		{
			proc = MidiInCallback;
			uint midiDeviceCount = CWin32.midiInGetNumDevs();
			Trace.TraceInformation("Number of MIDI input devices: {0}", midiDeviceCount);
			for (uint i = 0; i < midiDeviceCount; i++)
			{
				ConnectMidiDevice(i);
			}
		}
	}

	private CWin32.MIDIINCAPS caps;
	
	//scan connected MIDI devices, remove disconnected devices and add newly connected ones
	public void ScanDevices()
	{
		lock (objMidiInMutex)
		{
			if (isDisposed) return;

			uint nMidiDevices = CWin32.midiInGetNumDevs();

			//remove midi devices that are no longer connected
			for (int i = listInputDevices.Count - 1; i >= 0; i--)
			{
				if (listInputDevices[i] is CInputMIDI midiDevice)
				{
					//device is no longer present
					if (midiDevice.ID >= nMidiDevices)
					{
						DisconnectMidiDevice(midiDevice);
						listInputDevices.RemoveAt(i);
					}
					else //device is present, check if its still valid
					{
						uint result = CWin32.midiInGetDevCaps((uint)midiDevice.ID, ref caps, (uint)Marshal.SizeOf(caps));
						if (result != 0)
						{
							DisconnectMidiDevice(midiDevice);
							listInputDevices.RemoveAt(i);
						}
					}
				}
			}

			//add newly connected devices
			for (uint i = 0; i < nMidiDevices; i++)
			{
				if (!connectedMidiDevices.ContainsKey(i))
				{
					ConnectMidiDevice(i);
				}
			}
		}
	}

	//connect a new MIDI device by its ID
	//returns true if the device was successfully connected, false otherwise
	private Dictionary<uint, CInputMIDI> connectedMidiDevices = new();
	
	private bool ConnectMidiDevice(uint id)
	{
		uint result = CWin32.midiInGetDevCaps(id, ref caps, (uint)Marshal.SizeOf(caps));
		if (result != 0)
		{
			Trace.TraceError("MIDI In: Device {0}: midiInGetDevCaps() failed with error 0x{1:X2}", id, result);
			return false;
		}

		CInputMIDI newMidi = new(id);
		if (CWin32.midiInOpen(ref newMidi.hMidiIn, id, proc, IntPtr.Zero, 0x30000) != 0 ||
		    newMidi.hMidiIn == IntPtr.Zero)
		{
			Trace.TraceWarning("MIDI In: [{0}] \"{1}\" failed to connect.", id, caps.szPname);
			newMidi.Dispose();
			return false;
		}

		CWin32.midiInStart(newMidi.hMidiIn);
		newMidi.strDeviceName = caps.szPname;
		connectedMidiDevices[id] = newMidi;
		Trace.TraceInformation("MIDI In: [{0}] \"{1}\" has been connected and input started.", id, caps.szPname);
		return true;
	}
	
	private void DisconnectMidiDevice(CInputMIDI midiDevice)
	{
		if (midiDevice.hMidiIn != IntPtr.Zero)
		{
			CWin32.midiInStop(midiDevice.hMidiIn);
			CWin32.midiInReset(midiDevice.hMidiIn);
			CWin32.midiInClose(midiDevice.hMidiIn);
		}

		connectedMidiDevices.Remove((uint)midiDevice.ID);
		Trace.TraceInformation("MIDI In: [{0}] \"{1}\" has been disconnected.", midiDevice.ID, midiDevice.strDeviceName);
		midiDevice.Dispose();
	}

	public IInputDevice Joystick(int ID)
	{
		foreach (IInputDevice device in listInputDevices)
		{
			if ((device.eInputDeviceType == EInputDeviceType.Joystick) && (device.ID == ID))
			{
				return device;
			}
		}

		return null;
	}

	public IInputDevice Joystick(string GUID)
	{
		foreach (IInputDevice device in listInputDevices)
		{
			if ((device.eInputDeviceType == EInputDeviceType.Joystick) && device.GUID.Equals(GUID))
			{
				return device;
			}
		}

		return null;
	}

	public IInputDevice MidiIn(int ID)
	{
		foreach (IInputDevice device in listInputDevices)
		{
			if ((device.eInputDeviceType == EInputDeviceType.MidiIn) && (device.ID == ID))
			{
				return device;
			}
		}

		return null;
	}

	public void tPolling(bool isWindowActive, bool useBufferedInput) // tポーリング
	{
		lock (objMidiInMutex)
		{
			//				foreach( IInputDevice device in this.list入力デバイス )
			for (int i = listInputDevices.Count - 1;
			     i >= 0;
			     i--) // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
			{
				IInputDevice device = listInputDevices[i];
				try
				{
					device.tPolling(isWindowActive, useBufferedInput);
				}
				catch (SharpDX.SharpDXException
				       e) // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
				{
					if (e.ResultCode == ResultCode.OtherApplicationHasPriority)
					{
						// #xxxxx: 2017.5.9: from: このエラーの時は、何もしない。
					}
					else
					{
						// #xxxxx: 2017.5.9: from: その他のエラーの場合は、デバイスが外されたと想定してRemoveする。
						listInputDevices.Remove(device);
						device.Dispose();
						Trace.TraceError("Failed to poll device [{0}]. The device was probably disconnected. {1}",
							device.ID, e.Message);
					}
				}
			}
		}
	}

	#region [ IDisposable＋α ]

	//-----------------
	public void Dispose()
	{
		Dispose(true);
	}

	private void Dispose(bool disposeManagedObjects)
	{
		if (!isDisposed)
		{
			if (disposeManagedObjects)
			{
				foreach (CInputMIDI device in listInputDevices.OfType<CInputMIDI>().ToList())
				{
					DisconnectMidiDevice(device);
				}

				foreach (IInputDevice device2 in listInputDevices)
				{
					device2.Dispose();
				}

				lock (objMidiInMutex)
				{
					listInputDevices.Clear();
				}

				directInput.Dispose();

				//if( this.timer != null )
				//{
				//    this.timer.Dispose();
				//    this.timer = null;
				//}
			}

			isDisposed = true;
		}
	}

	~CInputManager()
	{
		Dispose(false);
		GC.KeepAlive(this);
	}

	//-----------------

	#endregion


	// その他

	#region [ private ]

	//-----------------
	private DirectInput directInput;
	private CInputKeyboard? _Keyboard;
	private CInputMouse? _Mouse;
	private bool isDisposed;
	private object objMidiInMutex = new();
	private CWin32.MidiInProc proc;
	//		private CTimer timer;

	private void MidiInCallback(IntPtr hMidiIn, uint wMsg, int dwInstance, int dwParam1, int dwParam2)
	{
		int p = dwParam1 & 0xF0;
		if (wMsg != CWin32.MIM_DATA || (p != 0x80 && p != 0x90 && p != 0xB0))
			return;

		long time = CSoundManager.rcPerformanceTimer.nシステム時刻; // lock前に取得。演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。

		lock (objMidiInMutex)
		{
			if ((listInputDevices != null) && (listInputDevices.Count != 0))
			{
				foreach (IInputDevice device in listInputDevices)
				{
					if ((device is CInputMIDI tmidi) && (tmidi.hMidiIn == hMidiIn))
					{
						tmidi.tメッセージからMIDI信号のみ受信(wMsg, dwInstance, dwParam1, dwParam2, time);
						break;
					}
				}
			}
		}
	}

	//-----------------

	#endregion
}