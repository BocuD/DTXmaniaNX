using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX.DirectInput;

namespace FDK;

public class CInputManager : IDisposable  // CInput管理
{
	// 定数

	public static int n通常音量 = 110;


	// プロパティ

	public List<IInputDevice> listInputDevices  // list入力デバイス
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
					_Keyboard = (CInputKeyboard) device;
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
					_Mouse = (CInputMouse) device;
					return _Mouse;
				}
			}
			return null;
		}
	}


	// コンストラクタ
	public CInputManager(IntPtr hWnd, bool bUseMidiIn = true)
	{
		InitializeInputManager(hWnd, bUseMidiIn);
	}

	private void InitializeInputManager(IntPtr hWnd, bool bUseMidiIn)
	{
		directInput = new DirectInput();
		listInputDevices = new List<IInputDevice>(10);
		#region [ Enumerate keyboard/mouse: exception is masked if keyboard/mouse is not connected ]
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
		#endregion
		
		#region [ Enumerate joypad ]
		foreach (DeviceInstance instance in directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
		{
			listInputDevices.Add(new CInputJoystick(hWnd, instance, directInput));
		}
		#endregion
		
		if (bUseMidiIn)
		{
			proc = MidiInCallback;
			uint midiDeviceCount = CWin32.midiInGetNumDevs();
			Trace.TraceInformation("Number of MIDI input devices: {0}", midiDeviceCount);

			for (uint i = 0; i < midiDeviceCount; i++)
			{
				CInputMIDI midiDevice = new(i);
				CWin32.MIDIINCAPS midiCaps = new();
				uint result = CWin32.midiInGetDevCaps(i, ref midiCaps, (uint)Marshal.SizeOf(midiCaps));

				if (result != 0)
				{
					Trace.TraceError("MIDI In: Device {0}: midiInGetDevCaps() failed with error 0x{1:X2}.", i, result);
				}
				else if (CWin32.midiInOpen(ref midiDevice.hMidiIn, i, proc, IntPtr.Zero, 0x30000) == 0 && midiDevice.hMidiIn != IntPtr.Zero)
				{
					CWin32.midiInStart(midiDevice.hMidiIn);
					Trace.TraceInformation("MIDI In: [{0}] \"{1}\" input started successfully.", i, midiCaps.szPname);
				}
				else
				{
					Trace.TraceError("MIDI In: [{0}] \"{1}\" failed to start input.", i, midiCaps.szPname);
				}

				midiDevice.strDeviceName = midiCaps.szPname;
				listInputDevices.Add(midiDevice);
			}
		}
	}


	// メソッド
	private CWin32.MIDIINCAPS caps;
	public void ScanDevices()
	{
		lock (objMidiInMutex)
		{
			if (isDisposed) return;
			
			uint nMidiDevices = CWin32.midiInGetNumDevs();

			for (uint i = 0; i < nMidiDevices; i++)
			{
				//skip if this device ID is already in the list
				bool alreadyExists = false;
				foreach (IInputDevice t in listInputDevices)
				{
					if (t is not CInputMIDI midi || midi.ID != i) continue;
					alreadyExists = true;
					break;
				}
				if (alreadyExists)
				{
					continue;
				}

				//try to reinitialize MIDI device
				uint result = CWin32.midiInGetDevCaps(i, ref caps, (uint)Marshal.SizeOf(caps));
				if (result != 0)
				{
					Trace.TraceError("MIDI In: Device {0}: midiInGetDevCaps() failed with error 0x{1:X2}", i, result);
					continue;
				}

				CInputMIDI newMidi = new(i);
				if (CWin32.midiInOpen(ref newMidi.hMidiIn, i, proc, IntPtr.Zero, 0x30000) == 0 && newMidi.hMidiIn != IntPtr.Zero)
				{
					CWin32.midiInStart(newMidi.hMidiIn);
					newMidi.strDeviceName = caps.szPname;
					listInputDevices.Add(newMidi);
					Trace.TraceInformation("MIDI In: [{0}] \"{1}\" has been reconnected and input has been started.", i, caps.szPname);
				}
				else
				{
					Trace.TraceWarning("MIDI In: [{0}] \"{1}\" failed to reconnect.", i, caps.szPname);
				}
			}
		}
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
	public void tPolling(bool isWindowActive, bool useBufferedInput)  // tポーリング
	{
		lock (objMidiInMutex)
		{
			//				foreach( IInputDevice device in this.list入力デバイス )
			for (int i = listInputDevices.Count - 1; i >= 0; i--)    // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
			{
				IInputDevice device = listInputDevices[i];
				try
				{
					device.tPolling(isWindowActive, useBufferedInput);
				}
				catch (SharpDX.SharpDXException e)                                      // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
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
						Trace.TraceError("Failed to poll device [{0}]. The device was probably disconnected. {1}", device.ID, e.Message);
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
				foreach (IInputDevice device in listInputDevices)
				{
					CInputMIDI tmidi = device as CInputMIDI;
					if (tmidi != null)
					{
						CWin32.midiInStop(tmidi.hMidiIn);
						CWin32.midiInReset(tmidi.hMidiIn);
						CWin32.midiInClose(tmidi.hMidiIn);
						Trace.TraceInformation("MIDI In: [{0}] を停止しました。", new object[] { tmidi.ID });
					}
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

		long time = CSoundManager.rcPerformanceTimer.nシステム時刻;  // lock前に取得。演奏用タイマと同じタイマを使うことで、BGMと譜面、入力ずれを防ぐ。

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