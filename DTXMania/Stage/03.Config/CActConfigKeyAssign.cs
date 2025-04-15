using System.Runtime.InteropServices;
using DTXMania.Core;
using FDK;
using Rectangle = System.Drawing.Rectangle;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CActConfigKeyAssign : CActivity
{
	// プロパティ

	public bool bキー入力待ちの最中である => bWaitingForKeyInput;


	// メソッド

	public void tStart( EKeyConfigPart part, EKeyConfigPad pad, string strパッド名 )
	{
		if( part != EKeyConfigPart.UNKNOWN )
		{
			this.part = part;
			this.pad = pad;
			this.strパッド名 = strパッド名;
			for( int i = 0; i < 16; i++ )
			{
				structReset用KeyAssign[ i ].InputDevice = CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].InputDevice;
				structReset用KeyAssign[ i ].ID = CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].ID;
				structReset用KeyAssign[ i ].Code = CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].Code;
			}
		}
	}
		
	public void tPressEnter()
	{
		if( !bWaitingForKeyInput )
		{
			CDTXMania.Skin.soundDecide.tPlay();
			switch( nSelectedRow )
			{
				case 16:
					for( int i = 0; i < 16; i++ )
					{
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].InputDevice = structReset用KeyAssign[ i ].InputDevice;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].ID = structReset用KeyAssign[ i ].ID;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].Code = structReset用KeyAssign[ i ].Code;
					}
					return;

				case 0x11:
					stageConfig.tNotifyAssignmentComplete();
					return;
			}
			bWaitingForKeyInput = true;
		}
	}
	public void tMoveToNext()
	{
		if( !bWaitingForKeyInput )
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nSelectedRow = ( nSelectedRow + 1 ) % 0x12;
		}
	}
	public void tMoveToPrevious()
	{
		if( !bWaitingForKeyInput )
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nSelectedRow = ( ( nSelectedRow - 1 ) + 0x12 ) % 0x12;
		}
	}

		
	// CActivity 実装

	public override void OnActivate()
	{
		part = EKeyConfigPart.UNKNOWN;
		pad = EKeyConfigPad.UNKNOWN;
		strパッド名 = "";
		nSelectedRow = 0;
		bWaitingForKeyInput = false;
		structReset用KeyAssign = new CConfigIni.CKeyAssign.STKEYASSIGN[ 0x10 ];
		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		if( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref txカーソル );
			CDTXMania.tReleaseTexture( ref txHitKeyダイアログ );
			base.OnDeactivate();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			txカーソル = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenConfig menu cursor.png" ), false );
			txHitKeyダイアログ = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\ScreenConfig hit key to assign dialog.png" ), false );
			base.OnManagedCreateResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( !bNotActivated )
		{
			if( bWaitingForKeyInput )
			{
				if( CDTXMania.InputManager.Keyboard.bKeyPressed( (int)SlimDXKey.Escape ) )
				{
					CDTXMania.Skin.soundCancel.tPlay();
					bWaitingForKeyInput = false;
					CDTXMania.InputManager.tPolling( CDTXMania.app.bApplicationActive, false );
				}
				else if( ( tキーチェックとアサイン_Keyboard() || tキーチェックとアサイン_MidiIn() ) || ( tキーチェックとアサイン_Joypad() || tキーチェックとアサイン_Mouse() ) )
				{
					bWaitingForKeyInput = false;
					CDTXMania.InputManager.tPolling( CDTXMania.app.bApplicationActive, false );
				}
			}
			else if( ( CDTXMania.InputManager.Keyboard.bKeyPressed( (int)SlimDXKey.Delete ) && ( nSelectedRow >= 0 ) ) && ( nSelectedRow <= 15 ) )
			{
				CDTXMania.Skin.soundDecide.tPlay();
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].InputDevice = EInputDevice.Unknown;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].ID = 0;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].Code = 0;
			}
			if( txカーソル != null )
			{
				int num = 20;
				// 15SEP20 Increasing x position by 120 pixels (was 0x144)
				int cursPosX = 0x1bc;
				int cursPosY = 0x3e + ( num * ( nSelectedRow + 1 ) );
				txカーソル.tDraw2D( CDTXMania.app.Device, cursPosX, cursPosY, new Rectangle( 0, 0, 0x10, 0x20 ) );
				cursPosX += 0x10;
				Rectangle rectangle = new( 8, 0, 0x10, 0x20 );
				for( int j = 0; j < 14; j++ )
				{
					txカーソル.tDraw2D( CDTXMania.app.Device, cursPosX, cursPosY, rectangle );
					cursPosX += 0x10;
				}
				txカーソル.tDraw2D( CDTXMania.app.Device, cursPosX, cursPosY, new Rectangle( 0x10, 0, 0x10, 0x20 ) );
			}
			int num5 = 20;
			// 15SEP20 Increasing x position by 120 pixels (was 0x134)
			int x = 0x1ac;
			int y = 0x40;
			stageConfig.actFont.t文字列描画( x, y, strパッド名, false, 0.75f );
			y += num5;
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			for( int i = 0; i < 0x10; i++ )
			{
				switch( stkeyassignArray[ i ].InputDevice )
				{
					case EInputDevice.Keyboard:
						tアサインコードの描画_Keyboard( i + 1, x + 20, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].Code, nSelectedRow == i );
						break;

					case EInputDevice.MIDI入力:
						tアサインコードの描画_MidiIn( i + 1, x + 20, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].Code, nSelectedRow == i );
						break;

					case EInputDevice.Joypad:
						tアサインコードの描画_Joypad( i + 1, x + 20, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].Code, nSelectedRow == i );
						break;

					case EInputDevice.Mouse:
						tアサインコードの描画_Mouse( i + 1, x + 20, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].Code, nSelectedRow == i );
						break;

					default:
						stageConfig.actFont.t文字列描画( x + 20, y, string.Format( "{0,2}.", i + 1 ), nSelectedRow == i, 0.75f );
						break;
				}
				y += num5;
			}
			stageConfig.actFont.t文字列描画( x + 20, y, "Reset", nSelectedRow == 0x10, 0.75f );
			y += num5;
			stageConfig.actFont.t文字列描画( x + 20, y, "<< Returnto List", nSelectedRow == 0x11, 0.75f );
			y += num5;
			if( bWaitingForKeyInput && ( txHitKeyダイアログ != null ) )
			{
				// 15SEP20 Increasing x position by 120 pixels (was 0x185)
				txHitKeyダイアログ.tDraw2D( CDTXMania.app.Device, 0x1fd, 0xd7 );
			}
		}
		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	[StructLayout( LayoutKind.Sequential )]
	private struct STKEYLABEL
	{
		public int nCode;
		public string strLabel;
		public STKEYLABEL( int nCode, string strLabel )
		{
			this.nCode = nCode;
			this.strLabel = strLabel;
		}
	}

	private bool bWaitingForKeyInput;
	private STKEYLABEL[] KeyLabel = new STKEYLABEL[] { 
		new(0x35, "[ESC]"),
		new(1, "[ 1 ]"),
		new(2, "[ 2 ]"),
		new(3, "[ 3 ]"),
		new(4, "[ 4 ]"),
		new(5, "[ 5 ]"),
		new(6, "[ 6 ]"),
		new(7, "[ 7 ]"),
		new(8, "[ 8 ]"),
		new(9, "[ 9 ]"),
		new(0, "[ 0 ]"),
		new(0x53, "[ - ]"),
		new(0x34, "[ = ]"),
		new(0x2a, "[BSC]"),
		new(0x81, "[TAB]"),
		new(0x1a, "[ Q ]"), 
		new(0x20, "[ W ]"),
		new(14, "[ E ]"),
		new(0x1b, "[ R ]"),
		new(0x1d, "[ T ]"),
		new(0x22, "[ Y ]"),
		new(30, "[ U ]"),
		new(0x12, "[ I ]"),
		new(0x18, "[ O ]"),
		new(0x19, "[ P ]"),
		new(0x4a, "[ [ ]"),
		new(0x73, "[ ] ]"),
		new(0x75, "[Enter]"),
		new(0x4b, "[L-Ctrl]"),
		new(10, "[ A ]"),
		new(0x1c, "[ S ]"),
		new(13, "[ D ]"), 
		new(15, "[ F ]"),
		new(0x10, "[ G ]"),
		new(0x11, "[ H ]"),
		new(0x13, "[ J ]"),
		new(20, "[ K ]"),
		new(0x15, "[ L ]"),
		new(0x7b, "[ ; ]"),
		new(0x26, "[ ' ]"),
		new(0x45, "[ ` ]"),
		new(0x4e, "[L-Shift]"),
		new(0x2b, @"[ \]"),
		new(0x23, "[ Z ]"),
		new(0x21, "[ X ]"),
		new(12, "[ C ]"),
		new(0x1f, "[ V ]"),
		new(11, "[ B ]"), 
		new(0x17, "[ N ]"),
		new(0x16, "[ M ]"),
		new(0x2f, "[ , ]"),
		new(0x6f, "[ . ]"),
		new(0x7c, "[ / ]"),
		new(120, "[R-Shift]"),
		new(0x6a, "[ * ]"),
		new(0x4d, "[L-Alt]"),
		new(0x7e, "[Space]"),
		new(0x2d, "[CAPS]"),
		new(0x36, "[F1]"),
		new(0x37, "[F2]"),
		new(0x38, "[F3]"),
		new(0x39, "[F4]"),
		new(0x3a, "[F5]"),
		new(0x3b, "[F6]"), 
		new(60, "[F7]"),
		new(0x3d, "[F8]"),
		new(0x3e, "[F9]"),
		new(0x3f, "[F10]"),
		new(0x58, "[NumLock]"),
		new(0x7a, "[Scroll]"),
		new(0x60, "[NPad7]"),
		new(0x61, "[NPad8]"),
		new(0x62, "[NPad9]"),
		new(0x66, "[NPad-]"),
		new(0x5d, "[NPad4]"),
		new(0x5e, "[NPad5]"),
		new(0x5f, "[NPad6]"),
		new(0x68, "[NPad+]"),
		new(90, "[NPad1]"),
		new(0x5b, "[NPad2]"), 
		new(0x5c, "[NPad3]"),
		new(0x59, "[NPad0]"),
		new(0x67, "[NPad.]"),
		new(0x40, "[F11]"),
		new(0x41, "[F12]"),
		new(0x42, "[F13]"),
		new(0x43, "[F14]"),
		new(0x44, "[F15]"),
		new(0x48, "[Kana]"),
		new(0x24, "[ ? ]"),
		new(0x30, "[Henkan]"),
		new(0x57, "[MuHenkan]"),
		new(0x8f, @"[ \ ]"),
		new(0x25, "[NPad.]"),
		new(0x65, "[NPad=]"),
		new(0x72, "[ ^ ]"), 
		new(40, "[ @ ]"),
		new(0x2e, "[ : ]"),
		new(130, "[ _ ]"),
		new(0x49, "[Kanji]"),
		new(0x7f, "[Stop]"),
		new(0x29, "[AX]"),
		new(100, "[NPEnter]"),
		new(0x74, "[R-Ctrl]"),
		new(0x54, "[Mute]"),
		new(0x2c, "[Calc]"),
		new(0x70, "[PlayPause]"),
		new(0x52, "[MediaStop]"),
		new(0x85, "[Volume-]"),
		new(0x86, "[Volume+]"),
		new(0x8b, "[WebHome]"),
		new(0x63, "[NPad,]"), 
		new(0x69, "[ / ]"),
		new(0x80, "[PrtScn]"),
		new(0x77, "[R-Alt]"),
		new(110, "[Pause]"),
		new(70, "[Home]"),
		new(0x84, "[Up]"),
		new(0x6d, "[PageUp]"),
		new(0x4c, "[Left]"),
		new(0x76, "[Right]"),
		new(0x33, "[End]"),
		new(50, "[Down]"),
		new(0x6c, "[PageDown]"),
		new(0x47, "[Insert]"),
		new(0x31, "[Delete]"),
		new(0x4f, "[L-Win]"),
		new(0x79, "[R-Win]"), 
		new(0x27, "[APP]"),
		new(0x71, "[Power]"),
		new(0x7d, "[Sleep]"),
		new(0x87, "[Wake]")
	};
	private int nSelectedRow;
	private EKeyConfigPad pad;
	private EKeyConfigPart part;
	private CConfigIni.CKeyAssign.STKEYASSIGN[] structReset用KeyAssign;
	private string strパッド名;
	private CTexture txHitKeyダイアログ;
	private CTexture txカーソル;

	private CStageConfig stageConfig;

	public CActConfigKeyAssign(CStageConfig cStageConfig)
	{
		stageConfig = cStageConfig;
	}

	private void tアサインコードの描画_Joypad( int line, int x, int y, int nID, int nCode, bool b強調 )
	{
		string str = "";
		switch( nCode )
		{
			case 0:
				str = "Left";
				break;

			case 1:
				str = "Right";
				break;

			case 2:
				str = "Up";
				break;

			case 3:
				str = "Down";
				break;

			case 4:
				str = "Forward";
				break;

			case 5:
				str = "Back";
				break;

			default:
				if( ( 6 <= nCode ) && ( nCode < 6 + 128 ) )				// other buttons (128 types)
				{
					str = string.Format( "Button{0}", nCode - 5 );
				}
				else if ( ( 6 + 128 <= nCode ) && ( nCode < 6 + 128 + 8 ) )		// POV HAT ( 8 types; 45 degrees per HATs)
				{
					str = string.Format( "POV {0}", ( nCode - 6 - 128 ) * 45 );
				}
				else
				{
					str = string.Format( "Code{0}", nCode );
				}
				break;
		}
		stageConfig.actFont.t文字列描画( x, y, string.Format( "{0,2}. Joypad #{1} ", line, nID ) + str, b強調, 0.75f );
	}
	private void tアサインコードの描画_Keyboard( int line, int x, int y, int nID, int nCode, bool b強調 )
	{
		string str = null;
		foreach( STKEYLABEL stkeylabel in KeyLabel )
		{
			if( stkeylabel.nCode == nCode )
			{
				str = string.Format( "{0,2}. Key {1}", line, stkeylabel.strLabel );
				break;
			}
		}
		if( str == null )
		{
			str = string.Format( "{0,2}. Key 0x{1:X2}", line, nCode );
		}
		stageConfig.actFont.t文字列描画( x, y, str, b強調, 0.75f );
	}
	private void tアサインコードの描画_MidiIn( int line, int x, int y, int nID, int nCode, bool b強調 )
	{
		stageConfig.actFont.t文字列描画( x, y, string.Format( "{0,2}. MidiIn #{1} code.{2}", line, nID, nCode ), b強調, 0.75f );
	}
	private void tアサインコードの描画_Mouse( int line, int x, int y, int nID, int nCode, bool b強調 )
	{
		stageConfig.actFont.t文字列描画( x, y, string.Format( "{0,2}. Mouse Button{1}", line, nCode ), b強調, 0.75f );
	}
	private bool tキーチェックとアサイン_Joypad()
	{
		foreach( IInputDevice device in CDTXMania.InputManager.listInputDevices )
		{
			if( device.eInputDeviceType == EInputDeviceType.Joystick )
			{
				for( int i = 0; i < 6 + 0x80 + 8; i++ )		// +6 for Axis, +8 for HAT
				{
					if( device.bKeyPressed( i ) )
					{
						CDTXMania.Skin.soundDecide.tPlay();
						CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs( EInputDevice.Joypad, device.ID, i );
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].InputDevice = EInputDevice.Joypad;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].ID = device.ID;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].Code = i;
						return true;
					}
				}
			}
		}
		return false;
	}
	private bool tキーチェックとアサイン_Keyboard()
	{
		for( int i = 0; i < 0x100; i++ )
		{
			if( i != (int)SlimDXKey.Escape &&
			    i != (int)SlimDXKey.UpArrow &&
			    i != (int)SlimDXKey.DownArrow &&
			    i != (int)SlimDXKey.LeftArrow &&
			    i != (int)SlimDXKey.RightArrow &&
			    i != (int)SlimDXKey.Delete &&
			    i != (int)SlimDXKey.Return &&
			    CDTXMania.InputManager.Keyboard.bKeyPressed( i ) )
			{
				CDTXMania.Skin.soundDecide.tPlay();
				CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs( EInputDevice.Keyboard, 0, i );
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].InputDevice = EInputDevice.Keyboard;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].ID = 0;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].Code = i;
				return true;
			}
		}
		return false;
	}
	private bool tキーチェックとアサイン_MidiIn()
	{
		foreach( IInputDevice device in CDTXMania.InputManager.listInputDevices )
		{
			if( device.eInputDeviceType == EInputDeviceType.MidiIn )
			{
				for( int i = 0; i < 0x100; i++ )
				{
					if( device.bKeyPressed( i ) )
					{
						CDTXMania.Skin.soundDecide.tPlay();
						CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs( EInputDevice.MIDI入力, device.ID, i );
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].InputDevice = EInputDevice.MIDI入力;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].ID = device.ID;
						CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].Code = i;
						return true;
					}
				}
			}
		}
		return false;
	}
	private bool tキーチェックとアサイン_Mouse()
	{
		for( int i = 0; i < 8; i++ )
		{
			if( CDTXMania.InputManager.Mouse.bKeyPressed( i ) )
			{
				CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs( EInputDevice.Mouse, 0, i );
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].InputDevice = EInputDevice.Mouse;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].ID = 0;
				CDTXMania.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ nSelectedRow ].Code = i;
			}
		}
		return false;
	}
	//-----------------
	#endregion
}