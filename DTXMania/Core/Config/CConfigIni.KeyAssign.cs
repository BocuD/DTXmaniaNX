using System.Runtime.InteropServices;

namespace DTXMania.Core;

internal partial class CConfigIni
{
	public class CKeyAssign
	{
		///<summary>Maximum number of input bindings that can be assigned to a single pad/button.</summary>
		public const int KeyAssignsPerPad = 16;

		[StructLayout(LayoutKind.Sequential)]
		public struct STKEYASSIGN
		{
			public EInputDevice InputDevice;
			public int ID;
			public int Code;

			public STKEYASSIGN(EInputDevice DeviceType, int nID, int nCode)
			{
				InputDevice = DeviceType;
				ID = nID;
				Code = nCode;
			}
		}

		/// <summary>
		/// The bindings for one part. Pads are stored in a single array indexed by <see cref="EKeyConfigPad"/>;
		/// the named properties below are thin accessors over it. Because aliases such as drums HH and
		/// guitar R share an <see cref="EKeyConfigPad"/> value, they automatically resolve to the same slot —
		/// no duplicated backing fields required.
		/// </summary>
		public class CKeyAssignPad
		{
			private readonly STKEYASSIGN[][] pads;

			public CKeyAssignPad()
			{
				pads = new STKEYASSIGN[(int)EKeyConfigPad.MAX][];
				for (int pad = 0; pad < pads.Length; pad++)
				{
					pads[pad] = new STKEYASSIGN[KeyAssignsPerPad];
					for (int i = 0; i < KeyAssignsPerPad; i++)
					{
						pads[pad][i] = new STKEYASSIGN(EInputDevice.Unknown, 0, 0);
					}
				}
			}

			public STKEYASSIGN[] this[int index]
			{
				get => pads[index];
				set => pads[index] = value;
			}

			// Named accessors. Drum names and their guitar/bass aliases (HH/R, SD/G, BD/B, HT/Pick,
			// LT/Wail, FT/Help, CY/Decide, HHO/Y, LC/P) map to the same EKeyConfigPad value and so
			// share a slot automatically.
			public STKEYASSIGN[] HH { get => this[(int)EKeyConfigPad.HH]; set => this[(int)EKeyConfigPad.HH] = value; }
			public STKEYASSIGN[] R { get => this[(int)EKeyConfigPad.R]; set => this[(int)EKeyConfigPad.R] = value; }
			public STKEYASSIGN[] SD { get => this[(int)EKeyConfigPad.SD]; set => this[(int)EKeyConfigPad.SD] = value; }
			public STKEYASSIGN[] G { get => this[(int)EKeyConfigPad.G]; set => this[(int)EKeyConfigPad.G] = value; }
			public STKEYASSIGN[] BD { get => this[(int)EKeyConfigPad.BD]; set => this[(int)EKeyConfigPad.BD] = value; }
			public STKEYASSIGN[] B { get => this[(int)EKeyConfigPad.B]; set => this[(int)EKeyConfigPad.B] = value; }
			public STKEYASSIGN[] HT { get => this[(int)EKeyConfigPad.HT]; set => this[(int)EKeyConfigPad.HT] = value; }
			public STKEYASSIGN[] Pick { get => this[(int)EKeyConfigPad.Pick]; set => this[(int)EKeyConfigPad.Pick] = value; }
			public STKEYASSIGN[] LT { get => this[(int)EKeyConfigPad.LT]; set => this[(int)EKeyConfigPad.LT] = value; }
			public STKEYASSIGN[] Wail { get => this[(int)EKeyConfigPad.Wail]; set => this[(int)EKeyConfigPad.Wail] = value; }
			public STKEYASSIGN[] FT { get => this[(int)EKeyConfigPad.FT]; set => this[(int)EKeyConfigPad.FT] = value; }
			public STKEYASSIGN[] Help { get => this[(int)EKeyConfigPad.Help]; set => this[(int)EKeyConfigPad.Help] = value; }
			public STKEYASSIGN[] CY { get => this[(int)EKeyConfigPad.CY]; set => this[(int)EKeyConfigPad.CY] = value; }
			public STKEYASSIGN[] Decide { get => this[(int)EKeyConfigPad.Decide]; set => this[(int)EKeyConfigPad.Decide] = value; }
			public STKEYASSIGN[] HHO { get => this[(int)EKeyConfigPad.HHO]; set => this[(int)EKeyConfigPad.HHO] = value; }
			public STKEYASSIGN[] Y { get => this[(int)EKeyConfigPad.Y]; set => this[(int)EKeyConfigPad.Y] = value; }
			public STKEYASSIGN[] RD { get => this[(int)EKeyConfigPad.RD]; set => this[(int)EKeyConfigPad.RD] = value; }
			public STKEYASSIGN[] P { get => this[(int)EKeyConfigPad.P]; set => this[(int)EKeyConfigPad.P] = value; }
			public STKEYASSIGN[] LC { get => this[(int)EKeyConfigPad.LC]; set => this[(int)EKeyConfigPad.LC] = value; }
			public STKEYASSIGN[] LP { get => this[(int)EKeyConfigPad.LP]; set => this[(int)EKeyConfigPad.LP] = value; }
			public STKEYASSIGN[] LBD { get => this[(int)EKeyConfigPad.LBD]; set => this[(int)EKeyConfigPad.LBD] = value; }
			public STKEYASSIGN[] Cancel { get => this[(int)EKeyConfigPad.Cancel]; set => this[(int)EKeyConfigPad.Cancel] = value; }
			public STKEYASSIGN[] Capture { get => this[(int)EKeyConfigPad.Capture]; set => this[(int)EKeyConfigPad.Capture] = value; }
			public STKEYASSIGN[] Search { get => this[(int)EKeyConfigPad.Search]; set => this[(int)EKeyConfigPad.Search] = value; }
			public STKEYASSIGN[] LoopCreate { get => this[(int)EKeyConfigPad.LoopCreate]; set => this[(int)EKeyConfigPad.LoopCreate] = value; }
			public STKEYASSIGN[] LoopDelete { get => this[(int)EKeyConfigPad.LoopDelete]; set => this[(int)EKeyConfigPad.LoopDelete] = value; }
			public STKEYASSIGN[] SkipForward { get => this[(int)EKeyConfigPad.SkipForward]; set => this[(int)EKeyConfigPad.SkipForward] = value; }
			public STKEYASSIGN[] SkipBackward { get => this[(int)EKeyConfigPad.SkipBackward]; set => this[(int)EKeyConfigPad.SkipBackward] = value; }
			public STKEYASSIGN[] IncreasePlaySpeed { get => this[(int)EKeyConfigPad.IncreasePlaySpeed]; set => this[(int)EKeyConfigPad.IncreasePlaySpeed] = value; }
			public STKEYASSIGN[] DecreasePlaySpeed { get => this[(int)EKeyConfigPad.DecreasePlaySpeed]; set => this[(int)EKeyConfigPad.DecreasePlaySpeed] = value; }
			public STKEYASSIGN[] Restart { get => this[(int)EKeyConfigPad.Restart]; set => this[(int)EKeyConfigPad.Restart] = value; }
		}

		public CKeyAssignPad Bass = new CKeyAssignPad();
		public CKeyAssignPad Drums = new CKeyAssignPad();
		public CKeyAssignPad Guitar = new CKeyAssignPad();
		public CKeyAssignPad System = new CKeyAssignPad();

		public CKeyAssignPad this[int index]
		{
			get
			{
				switch (index)
				{
					case (int)EKeyConfigPart.DRUMS: return Drums;
					case (int)EKeyConfigPart.GUITAR: return Guitar;
					case (int)EKeyConfigPart.BASS: return Bass;
					case (int)EKeyConfigPart.SYSTEM: return System;
				}
				throw new IndexOutOfRangeException();
			}
			set
			{
				switch (index)
				{
					case (int)EKeyConfigPart.DRUMS: Drums = value; return;
					case (int)EKeyConfigPart.GUITAR: Guitar = value; return;
					case (int)EKeyConfigPart.BASS: Bass = value; return;
					case (int)EKeyConfigPart.SYSTEM: System = value; return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}
}
