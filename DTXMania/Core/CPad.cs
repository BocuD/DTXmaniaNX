using System.Runtime.InteropServices;
using FDK;

namespace DTXMania.Core;

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
		rInputManager = mgrInput;
		stDetectedDevice.Clear();
	}


	// メソッド

	//todo: transitional, will be deleted with the guitar and bass swap removal
	//Key assignments are (still) stored per chart part, so a player has to use one
	//Storage will be migrated to per EPlayer instead, and then this mapping can go
	private static EInstrumentPart Slot(EPlayer player) => player switch
	{
		EPlayer.Drums => EInstrumentPart.DRUMS,
		EPlayer.Guitar1 => EInstrumentPart.GUITAR,
		_ => EInstrumentPart.BASS
	};

	public void GetEvents(EPlayer player, EPad pad, List<STInputEvent> list) => GetEvents(Slot(player), pad, list);

	public bool bPressed(EPlayer player, EPad pad) => bPressed(Slot(player), pad);

	public bool bPressing(EPlayer player, EPad pad) => bPressing(Slot(player), pad);

	/// </summary>
	public void GetEvents( EInstrumentPart part, EPad pad, List<STInputEvent> list )
	{
		list.Clear();
		tAddEvents( part, pad, list );

		if( pad == EPad.Pick && part != EInstrumentPart.DRUMS )
		{
			tAddEvents( part, EPad.PickUp, list );
			tAddEvents( part, EPad.PickDown, list );
		}
	}

	private void tAddEvents( EInstrumentPart part, EPad pad, List<STInputEvent> list )
	{
		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];

		// すべての入力デバイスについて…
		foreach (IInputDevice device in rInputManager.listInputDevices)
		{
			if ((device.listInputEvent != null) && (device.listInputEvent.Count != 0))
			{
				foreach (STInputEvent event2 in device.listInputEvent)
				{
					for (int i = 0; i < stkeyassignArray.Length; i++)
					{
						switch (stkeyassignArray[i].InputDevice)
						{
							case EInputDevice.Keyboard:
								if ((device.eInputDeviceType == EInputDeviceType.Keyboard) &&
								    (event2.nKey == stkeyassignArray[i].Code))
								{
									list.Add(event2);
									stDetectedDevice.Keyboard = true;
								}

								break;

							case EInputDevice.MIDI入力:
								if (((device.eInputDeviceType == EInputDeviceType.MidiIn) &&
								     (device.ID == stkeyassignArray[i].ID)) &&
								    (event2.nKey == stkeyassignArray[i].Code))
								{
									list.Add(event2);
									stDetectedDevice.MIDIIN = true;
								}

								break;

							case EInputDevice.Joypad:
								if (((device.eInputDeviceType == EInputDeviceType.Joystick) &&
								     (device.ID == stkeyassignArray[i].ID)) &&
								    (event2.nKey == stkeyassignArray[i].Code))
								{
									list.Add(event2);
									stDetectedDevice.Joypad = true;
								}

								break;

							case EInputDevice.Mouse:
								if ((device.eInputDeviceType == EInputDeviceType.Mouse) &&
								    (event2.nKey == stkeyassignArray[i].Code))
								{
									list.Add(event2);
									stDetectedDevice.Mouse = true;
								}

								break;
						}
					}
				}
			}
		}
	}
	public bool bPressed( EInstrumentPart part, EPad pad )
	{
		if ( part != EInstrumentPart.UNKNOWN )
		{
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			for( int i = 0; i < stkeyassignArray.Length; i++ )
			{
				switch( stkeyassignArray[ i ].InputDevice )
				{
					case EInputDevice.Keyboard:
						if ( !rInputManager.Keyboard.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.MIDI入力:
					{
						IInputDevice device2 = rInputManager.MidiIn( stkeyassignArray[ i ].ID );
						if ( ( device2 == null ) || !device2.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.MIDIIN = true;
						return true;
					}
					case EInputDevice.Joypad:
					{
						if ( !rConfigIni.joystickDict.ContainsKey( stkeyassignArray[ i ].ID ) )
							break;

						IInputDevice device = rInputManager.Joystick( stkeyassignArray[ i ].ID );
						if ( ( device == null ) || !device.bKeyPressed( stkeyassignArray[ i ].Code ) )
							break;

						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if ( !rInputManager.Mouse.bKeyPressed( stkeyassignArray[ i ].Code ) )
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
						if (!rInputManager.Keyboard.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.MIDI入力:
					{
						IInputDevice device2 = rInputManager.MidiIn(stkeyassignArray[i].ID);
						if ((device2 == null) || !device2.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.MIDIIN = true;
						return true;
					}
					case EInputDevice.Joypad:
					{
						if (!rConfigIni.joystickDict.ContainsKey(stkeyassignArray[i].ID))
							break;

						IInputDevice device = rInputManager.Joystick(stkeyassignArray[i].ID);
						if ((device == null) || !device.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if (!rInputManager.Mouse.bKeyPressed(stkeyassignArray[i].Code))
							break;

						stDetectedDevice.Mouse = true;
						return true;
				}
			}
		}
		return false;
	}
	public bool bPressedAny( EPad pad ) =>
		bPressed( EPlayer.Drums, pad ) || bPressed( EPlayer.Guitar1, pad ) || bPressed( EPlayer.Guitar2, pad );

	public bool bPressedGuitars( EPad pad ) =>
		bPressed( EPlayer.Guitar1, pad ) || bPressed( EPlayer.Guitar2, pad );
	public bool bPressing( EInstrumentPart part, EPad pad )
	{
		if ( part != EInstrumentPart.UNKNOWN )
		{
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			for( int i = 0; i < stkeyassignArray.Length; i++ )
			{
				switch( stkeyassignArray[ i ].InputDevice )
				{
					case EInputDevice.Keyboard:
						if ( !rInputManager.Keyboard.bKeyPressing( stkeyassignArray[ i ].Code ) )
						{
							break;
						}
						stDetectedDevice.Keyboard = true;
						return true;

					case EInputDevice.Joypad:
					{
						if ( !rConfigIni.joystickDict.ContainsKey( stkeyassignArray[ i ].ID ) )
						{
							break;
						}
						IInputDevice device = rInputManager.Joystick( stkeyassignArray[ i ].ID );
						if ( ( device == null ) || !device.bKeyPressing( stkeyassignArray[ i ].Code ) )
						{
							break;
						}
						stDetectedDevice.Joypad = true;
						return true;
					}
					case EInputDevice.Mouse:
						if ( !rInputManager.Mouse.bKeyPressing( stkeyassignArray[ i ].Code ) )
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
	public bool bPressingGuitars( EPad pad ) =>
		bPressing( EPlayer.Guitar1, pad ) || bPressing( EPlayer.Guitar2, pad );


	// Other

	#region [ private ]
	//-----------------
	private CConfigIni rConfigIni;
	private CInputManager rInputManager;
	//-----------------
	#endregion
}