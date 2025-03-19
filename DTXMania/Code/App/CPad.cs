using System.Runtime.InteropServices;
using FDK;

namespace DTXMania;

public class CPad
{
	// プロパティ

	internal STHIT stDetectedDevice;
	[StructLayout( LayoutKind.Sequential )]
	internal struct STHIT
	{
		public bool Keyboard;
		public bool MIDIIN;
		public bool Joypad;
		public bool Mouse;
		public void Clear()
		{
			Keyboard = false;
			MIDIIN = false;
			Joypad = false;
			Mouse = false;
		}
	}


	// コンストラクタ

	internal CPad( CConfigIni configIni, CInputManager mgrInput )
	{
		rConfigIni = configIni;
		rInput管理 = mgrInput;
		stDetectedDevice.Clear();
	}


	// メソッド

	public List<STInputEvent> GetEvents( EInstrumentPart part, EPad pad )
	{
		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
		List<STInputEvent> list = new List<STInputEvent>();

		// すべての入力デバイスについて…
		foreach( IInputDevice device in rInput管理.listInputDevices )
		{
			if( ( device.listInputEvent != null ) && ( device.listInputEvent.Count != 0 ) )
			{
				foreach( STInputEvent event2 in device.listInputEvent )
				{
					for( int i = 0; i < stkeyassignArray.Length; i++ )
					{
						switch( stkeyassignArray[ i ].InputDevice )
						{
							case EInputDevice.Keyboard:
								if( ( device.eInputDeviceType == EInputDeviceType.Keyboard ) && ( event2.nKey == stkeyassignArray[ i ].Code ) )
								{
									list.Add( event2 );
									stDetectedDevice.Keyboard = true;
								}
								break;

							case EInputDevice.MIDI入力:
								if( ( ( device.eInputDeviceType == EInputDeviceType.MidiIn ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].Code ) )
								{
									list.Add( event2 );
									stDetectedDevice.MIDIIN = true;
								}
								break;

							case EInputDevice.Joypad:
								if( ( ( device.eInputDeviceType == EInputDeviceType.Joystick ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].Code ) )
								{
									list.Add( event2 );
									stDetectedDevice.Joypad = true;
								}
								break;

							case EInputDevice.Mouse:
								if( ( device.eInputDeviceType == EInputDeviceType.Mouse ) && ( event2.nKey == stkeyassignArray[ i ].Code ) )
								{
									list.Add( event2 );
									stDetectedDevice.Mouse = true;
								}
								break;
						}
					}
				}
				continue;
			}
		}
		return list;
	}
	public bool bPressed( EInstrumentPart part, EPad pad )
	{
		if( part != EInstrumentPart.UNKNOWN )
		{
				
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			for( int i = 0; i < stkeyassignArray.Length; i++ )
			{
				switch( stkeyassignArray[ i ].InputDevice )
				{
					case EInputDevice.Keyboard:
						if( !rInput管理.Keyboard.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.MIDI入力:
					{
						IInputDevice device2 = rInput管理.MidiIn( stkeyassignArray[ i ].ID );
						if( ( device2 == null ) || !device2.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.MIDIIN = true;
						return true;
					}
					case EInputDevice.Joypad:
					{
						if( !rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
							break;

						IInputDevice device = rInput管理.Joystick( stkeyassignArray[ i ].ID );
						if( ( device == null ) || !device.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if( !rInput管理.Mouse.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.Mouse = true;
						return true;
				}
			}
		}
		return false;
	}
	public bool bPressed(EKeyConfigPart part, EKeyConfigPad pad)
	{
		if (part != EKeyConfigPart.UNKNOWN)
		{

			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[(int)part][(int)pad];
			for (int i = 0; i < stkeyassignArray.Length; i++)
			{
				switch (stkeyassignArray[i].InputDevice)
				{
					case EInputDevice.Keyboard:
						if (!rInput管理.Keyboard.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.MIDI入力:
					{
						IInputDevice device2 = rInput管理.MidiIn(stkeyassignArray[i].ID);
						if ((device2 == null) || !device2.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.MIDIIN = true;
						return true;
					}
					case EInputDevice.Joypad:
					{
						if (!rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID))
							break;

						IInputDevice device = rInput管理.Joystick(stkeyassignArray[i].ID);
						if ((device == null) || !device.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if (!rInput管理.Mouse.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Mouse = true;
						return true;
				}
			}
		}
		return false;
	}
	public bool bPressedDGB( EPad pad )
	{
		if( !bPressed( EInstrumentPart.DRUMS, pad ) && !bPressed( EInstrumentPart.GUITAR, pad ) )
		{
			return bPressed( EInstrumentPart.BASS, pad );
		}
		return true;
	}
	public bool bPressedGB( EPad pad )
	{
		if( !bPressed( EInstrumentPart.GUITAR, pad ) )
		{
			return bPressed( EInstrumentPart.BASS, pad );
		}
		return true;
	}
	public bool bPressing( EInstrumentPart part, EPad pad )
	{
		if( part != EInstrumentPart.UNKNOWN )
		{
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			for( int i = 0; i < stkeyassignArray.Length; i++ )
			{
				switch( stkeyassignArray[ i ].InputDevice )
				{
					case EInputDevice.Keyboard:
						if( !rInput管理.Keyboard.bKeyPressing( stkeyassignArray[ i ].Code ) )
						{
							break;
						}
						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.Joypad:
					{
						if( !rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
						{
							break;
						}
						IInputDevice device = rInput管理.Joystick( stkeyassignArray[ i ].ID );
						if( ( device == null ) || !device.bKeyPressing( stkeyassignArray[ i ].Code ) )
						{
							break;
						}
						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if( !rInput管理.Mouse.bKeyPressing( stkeyassignArray[ i ].Code ) )
						{
							break;
						}
						stDetectedDevice.Mouse = true;
						return true;
				}
			}
		}
		return false;
	}
	public bool bPressingGB( EPad pad )
	{
		if( !bPressing( EInstrumentPart.GUITAR, pad ) )
		{
			return bPressing( EInstrumentPart.BASS, pad );
		}
		return true;
	}


	// Other

	#region [ private ]
	//-----------------
	private CConfigIni rConfigIni;
	private CInputManager rInput管理;
	//-----------------
	#endregion
}