using System.Runtime.InteropServices;
using DTXMania.Core;
using FDK;
using SharpDX;
using Rectangle = System.Drawing.Rectangle;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CActConfigKeyAssign : CActivity
{
	public void tStart(EKeyConfigPart part, EKeyConfigPad pad, string strPadName)
	{
		if (part != EKeyConfigPart.UNKNOWN)
		{
			this.part = part;
			this.pad = pad;
			this.strPadName = strPadName;
			for (int i = 0; i < 16; i++)
			{
				structReset用KeyAssign[i].InputDevice = CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].InputDevice;
				structReset用KeyAssign[i].ID = CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].ID;
				structReset用KeyAssign[i].Code = CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].Code;
			}
		}
	}

	public void tPressEnter()
	{
		if (!bWaitingForKeyInput)
		{
			CDTXMania.Skin.soundDecide.tPlay();
			switch (nSelectedRow)
			{
				case 16:
					for (int i = 0; i < 16; i++)
					{
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].InputDevice =
							structReset用KeyAssign[i].InputDevice;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].ID = structReset用KeyAssign[i].ID;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][i].Code = structReset用KeyAssign[i].Code;
					}

					return;

				case 17:
					stageConfig.tNotifyAssignmentComplete();
					return;
			}

			bWaitingForKeyInput = true;
		}
	}

	public void tMoveToNext()
	{
		if (!bWaitingForKeyInput)
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nSelectedRow = (nSelectedRow + 1) % 18;
		}
	}

	public void tMoveToPrevious()
	{
		if (!bWaitingForKeyInput)
		{
			CDTXMania.Skin.soundCursorMovement.tPlay();
			nSelectedRow = ((nSelectedRow - 1) + 18) % 18;
		}
	}

	// CActivity 実装

	public override void OnActivate()
	{
		part = EKeyConfigPart.UNKNOWN;
		pad = EKeyConfigPad.UNKNOWN;
		strPadName = "";
		nSelectedRow = 0;
		bWaitingForKeyInput = false;
		structReset用KeyAssign = new CConfigIni.CKeyAssign.STKEYASSIGN[16];
		base.OnActivate();
	}

	public override void OnDeactivate()
	{
		if (bActivated)
		{
			CDTXMania.tReleaseTexture(ref txCursor);
			CDTXMania.tReleaseTexture(ref txHitKeyDialog);
			base.OnDeactivate();
		}
	}

	public override void OnManagedCreateResources()
	{
		if (bActivated)
		{
			txCursor = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenConfig menu cursor.png"), false);
			txHitKeyDialog =
				CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenConfig hit key to assign dialog.png"), false);
			base.OnManagedCreateResources();
		}
	}

	public override int OnUpdateAndDraw()
	{
		if (!bActivated) return 0;

		if (bWaitingForKeyInput)
		{
			if (CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Escape))
			{
				CDTXMania.Skin.soundCancel.tPlay();
				bWaitingForKeyInput = false;
				CDTXMania.InputManager.tPolling(CDTXMania.app.bApplicationActive, false);
			}
			else if (tKeyCheckAndAssignKeyboard() || tKeyCheckAndAssignMidiIn() ||
			         tKeyCheckAndAssignJoypad() || tKeyCheckAndAssignMouse())
			{
				bWaitingForKeyInput = false;
				CDTXMania.InputManager.tPolling(CDTXMania.app.bApplicationActive, false);
			}
		}
		else if (CDTXMania.InputManager.Keyboard.bKeyPressed((int)SlimDXKey.Delete) && nSelectedRow >= 0 &&
		         nSelectedRow <= 15)
		{
			CDTXMania.Skin.soundDecide.tPlay();
			CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].InputDevice = EInputDevice.Unknown;
			CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].ID = 0;
			CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].Code = 0;
		}

		if (txCursor != null)
		{
			int num = 20;
			// 15SEP20 Increasing x position by 120 pixels (was 0x144)
			int cursPosX = 444;
			int cursPosY = 62 + num * (nSelectedRow + 1);
			txCursor.tDraw2D(CDTXMania.app.Device, cursPosX, cursPosY, new RectangleF(0, 0, 16, 32));
			cursPosX += 16;
			RectangleF rectangle = new(8, 0, 16, 32);
			for (int j = 0; j < 14; j++)
			{
				txCursor.tDraw2D(CDTXMania.app.Device, cursPosX, cursPosY, rectangle);
				cursPosX += 16;
			}

			txCursor.tDraw2D(CDTXMania.app.Device, cursPosX, cursPosY, new RectangleF(16, 0, 16, 32));
		}

		int num5 = 20;
		// 15SEP20 Increasing x position by 120 pixels (was 0x134)
		int x = 428;
		int y = 64;
		stageConfig.actFont.t文字列描画(x, y, strPadName, false, 0.75f);
		y += num5;
		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad];
		for (int i = 0; i < 16; i++)
		{
			switch (stkeyassignArray[i].InputDevice)
			{
				case EInputDevice.Keyboard:
					tDrawAssignedCodeKeyboard(i + 1, x + 20, y, stkeyassignArray[i].ID, stkeyassignArray[i].Code,
						nSelectedRow == i);
					break;

				case EInputDevice.MIDI入力:
					tDrawAssignedCodeMidiIn(i + 1, x + 20, y, stkeyassignArray[i].ID, stkeyassignArray[i].Code,
						nSelectedRow == i);
					break;

				case EInputDevice.Joypad:
					tDrawAssignedCodeJoypad(i + 1, x + 20, y, stkeyassignArray[i].ID, stkeyassignArray[i].Code,
						nSelectedRow == i);
					break;

				case EInputDevice.Mouse:
					tDrawAssignedCodeMouse(i + 1, x + 20, y, stkeyassignArray[i].ID, stkeyassignArray[i].Code,
						nSelectedRow == i);
					break;

				default:
					stageConfig.actFont.t文字列描画(x + 20, y, $"{i + 1,2}.", nSelectedRow == i, 0.75f);
					break;
			}

			y += num5;
		}

		stageConfig.actFont.t文字列描画(x + 20, y, "Reset", nSelectedRow == 16, 0.75f);
		y += num5;
		stageConfig.actFont.t文字列描画(x + 20, y, "<< Return to List", nSelectedRow == 17, 0.75f);
		y += num5;
		if (bWaitingForKeyInput && txHitKeyDialog != null)
		{
			// 15SEP20 Increasing x position by 120 pixels (was 0x185)
			txHitKeyDialog.tDraw2D(CDTXMania.app.Device, 509, 215);
		}

		return 0;
	}


	// Other

	#region [ private ]

	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STKEYLABEL
	{
		public int nCode;
		public string strLabel;

		public STKEYLABEL(int nCode, string strLabel)
		{
			this.nCode = nCode;
			this.strLabel = strLabel;
		}
	}

	public bool bWaitingForKeyInput { get; private set; }

	private readonly STKEYLABEL[] keyLabel =
	[
		new(53, "[ESC]"),
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
		new(83, "[ - ]"),
		new(52, "[ = ]"),
		new(42, "[BSC]"),
		new(129, "[TAB]"),
		new(26, "[ Q ]"),
		new(32, "[ W ]"),
		new(14, "[ E ]"),
		new(27, "[ R ]"),
		new(29, "[ T ]"),
		new(34, "[ Y ]"),
		new(30, "[ U ]"),
		new(18, "[ I ]"),
		new(24, "[ O ]"),
		new(25, "[ P ]"),
		new(74, "[ [ ]"),
		new(115, "[ ] ]"),
		new(117, "[Enter]"),
		new(75, "[L-Ctrl]"),
		new(10, "[ A ]"),
		new(28, "[ S ]"),
		new(13, "[ D ]"),
		new(15, "[ F ]"),
		new(16, "[ G ]"),
		new(17, "[ H ]"),
		new(19, "[ J ]"),
		new(20, "[ K ]"),
		new(21, "[ L ]"),
		new(123, "[ ; ]"),
		new(38, "[ ' ]"),
		new(69, "[ ` ]"),
		new(78, "[L-Shift]"),
		new(43, @"[ \]"),
		new(35, "[ Z ]"),
		new(33, "[ X ]"),
		new(12, "[ C ]"),
		new(31, "[ V ]"),
		new(11, "[ B ]"),
		new(23, "[ N ]"),
		new(22, "[ M ]"),
		new(47, "[ , ]"),
		new(111, "[ . ]"),
		new(124, "[ / ]"),
		new(120, "[R-Shift]"),
		new(106, "[ * ]"),
		new(77, "[L-Alt]"),
		new(126, "[Space]"),
		new(45, "[CAPS]"),
		new(54, "[F1]"),
		new(55, "[F2]"),
		new(56, "[F3]"),
		new(57, "[F4]"),
		new(58, "[F5]"),
		new(59, "[F6]"),
		new(60, "[F7]"),
		new(61, "[F8]"),
		new(62, "[F9]"),
		new(63, "[F10]"),
		new(88, "[NumLock]"),
		new(122, "[Scroll]"),
		new(96, "[NPad7]"),
		new(97, "[NPad8]"),
		new(98, "[NPad9]"),
		new(102, "[NPad-]"),
		new(93, "[NPad4]"),
		new(94, "[NPad5]"),
		new(95, "[NPad6]"),
		new(104, "[NPad+]"),
		new(90, "[NPad1]"),
		new(91, "[NPad2]"),
		new(92, "[NPad3]"),
		new(89, "[NPad0]"),
		new(103, "[NPad.]"),
		new(64, "[F11]"),
		new(65, "[F12]"),
		new(66, "[F13]"),
		new(67, "[F14]"),
		new(68, "[F15]"),
		new(72, "[Kana]"),
		new(36, "[ ? ]"),
		new(48, "[Henkan]"),
		new(87, "[MuHenkan]"),
		new(143, @"[ \ ]"),
		new(37, "[NPad.]"),
		new(101, "[NPad=]"),
		new(114, "[ ^ ]"),
		new(40, "[ @ ]"),
		new(46, "[ : ]"),
		new(130, "[ _ ]"),
		new(73, "[Kanji]"),
		new(127, "[Stop]"),
		new(41, "[AX]"),
		new(100, "[NPEnter]"),
		new(116, "[R-Ctrl]"),
		new(84, "[Mute]"),
		new(44, "[Calc]"),
		new(112, "[PlayPause]"),
		new(82, "[MediaStop]"),
		new(133, "[Volume-]"),
		new(134, "[Volume+]"),
		new(139, "[WebHome]"),
		new(99, "[NPad,]"),
		new(105, "[ / ]"),
		new(128, "[PrtScn]"),
		new(119, "[R-Alt]"),
		new(110, "[Pause]"),
		new(70, "[Home]"),
		new(132, "[Up]"),
		new(109, "[PageUp]"),
		new(76, "[Left]"),
		new(118, "[Right]"),
		new(51, "[End]"),
		new(50, "[Down]"),
		new(108, "[PageDown]"),
		new(71, "[Insert]"),
		new(49, "[Delete]"),
		new(79, "[L-Win]"),
		new(121, "[R-Win]"),
		new(39, "[APP]"),
		new(113, "[Power]"),
		new(125, "[Sleep]"),
		new(135, "[Wake]")
	];

	private int nSelectedRow;
	private EKeyConfigPad pad;
	private EKeyConfigPart part;
	private CConfigIni.CKeyAssign.STKEYASSIGN[] structReset用KeyAssign;
	private string strPadName;
	private CTexture txHitKeyDialog;
	private CTexture txCursor;

	private CStageConfig stageConfig;

	public CActConfigKeyAssign(CStageConfig cStageConfig)
	{
		stageConfig = cStageConfig;
	}

	private void tDrawAssignedCodeJoypad(int line, int x, int y, int nID, int nCode, bool b強調)
	{
		string str = "";
		switch (nCode)
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
				switch (nCode)
				{
					// other buttons (128 types)
					case >= 6 and < 6 + 128:
						str = $"Button{nCode - 5}";
						break;
					
					// POV HAT ( 8 types; 45 degrees per HATs)
					case >= 6 + 128 and < 6 + 128 + 8:
						str = $"POV {(nCode - 6 - 128) * 45}";
						break;
					
					default:
						str = $"Code{nCode}";
						break;
				}

				break;
		}

		stageConfig.actFont.t文字列描画(x, y, $"{line,2}. Joypad #{nID} " + str, b強調, 0.75f);
	}

	private void tDrawAssignedCodeKeyboard(int line, int x, int y, int nID, int nCode, bool b強調)
	{
		string str = null;
		foreach (STKEYLABEL stkeylabel in keyLabel)
		{
			if (stkeylabel.nCode == nCode)
			{
				str = $"{line,2}. Key {stkeylabel.strLabel}";
				break;
			}
		}

		if (str == null)
		{
			str = $"{line,2}. Key 0x{nCode:X2}";
		}

		stageConfig.actFont.t文字列描画(x, y, str, b強調, 0.75f);
	}

	private void tDrawAssignedCodeMidiIn(int line, int x, int y, int nID, int nCode, bool b強調)
	{
		stageConfig.actFont.t文字列描画(x, y, $"{line,2}. MidiIn #{nID} code.{nCode}", b強調, 0.75f);
	}

	private void tDrawAssignedCodeMouse(int line, int x, int y, int nID, int nCode, bool b強調)
	{
		stageConfig.actFont.t文字列描画(x, y, $"{line,2}. Mouse Button{nCode}", b強調, 0.75f);
	}

	private bool tKeyCheckAndAssignJoypad()
	{
		foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
		{
			if (device.eInputDeviceType == EInputDeviceType.Joystick)
			{
				for (int i = 0; i < 6 + 128 + 8; i++) // +6 for Axis, +8 for HAT
				{
					if (device.bKeyPressed(i))
					{
						CDTXMania.Skin.soundDecide.tPlay();
						CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs(EInputDevice.Joypad, device.ID, i);
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].InputDevice =
							EInputDevice.Joypad;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].ID = device.ID;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].Code = i;
						return true;
					}
				}
			}
		}

		return false;
	}

	private bool tKeyCheckAndAssignKeyboard()
	{
		for (int i = 0; i < 256; i++)
		{
			if (i != (int)SlimDXKey.Escape &&
			    i != (int)SlimDXKey.UpArrow &&
			    i != (int)SlimDXKey.DownArrow &&
			    i != (int)SlimDXKey.LeftArrow &&
			    i != (int)SlimDXKey.RightArrow &&
			    i != (int)SlimDXKey.Delete &&
			    i != (int)SlimDXKey.Return &&
			    CDTXMania.InputManager.Keyboard.bKeyPressed(i))
			{
				CDTXMania.Skin.soundDecide.tPlay();
				CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs(EInputDevice.Keyboard, 0, i);
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].InputDevice = EInputDevice.Keyboard;
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].ID = 0;
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].Code = i;
				return true;
			}
		}

		return false;
	}

	private bool tKeyCheckAndAssignMidiIn()
	{
		foreach (IInputDevice device in CDTXMania.InputManager.listInputDevices)
		{
			if (device.eInputDeviceType == EInputDeviceType.MidiIn)
			{
				for (int i = 0; i < 256; i++)
				{
					if (device.bKeyPressed(i))
					{
						CDTXMania.Skin.soundDecide.tPlay();
						CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs(EInputDevice.MIDI入力, device.ID, i);
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].InputDevice =
							EInputDevice.MIDI入力;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].ID = device.ID;
						CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].Code = i;
						return true;
					}
				}
			}
		}

		return false;
	}

	private bool tKeyCheckAndAssignMouse()
	{
		for (int i = 0; i < 8; i++)
		{
			if (CDTXMania.InputManager.Mouse.bKeyPressed(i))
			{
				CDTXMania.ConfigIni.tDeleteAlreadyAssignedInputs(EInputDevice.Mouse, 0, i);
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].InputDevice = EInputDevice.Mouse;
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].ID = 0;
				CDTXMania.ConfigIni.KeyAssign[(int)part][(int)pad][nSelectedRow].Code = i;
			}
		}

		return false;
	}

	//-----------------

	#endregion
}