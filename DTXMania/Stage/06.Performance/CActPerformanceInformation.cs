using System.Globalization;
using DTXMania.Core;
using DTXMania.Core.Audio;
using FDK;

namespace DTXMania;

internal class CActPerformanceInformation : CActivity
{
	// プロパティ

	public double dbBPM;
	public int jl;
	public int n小節番号;
	public int nPERFECT数;
	public int nGREAT数;
	public int nGOOD数;
	public int nPOOR数;
	public int nMISS数;


	// コンストラクタ

	public CActPerformanceInformation()
	{
		bActivated = false;
	}

				
	// CActivity 実装

	public override void OnActivate()
	{
		jl = 0;
		n小節番号 = 0;
		dbBPM = CDTXMania.DTX.BASEBPM + CDTXMania.DTX.BPM;

		nPERFECT数 = 0;
		nGREAT数 = 0;
		nGOOD数 = 0;
		nPOOR数 = 0;
		nMISS数 = 0;
		base.OnActivate();
	}

	private void Line(int x, int y, Span<char> text, int written)
		=> CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, text[..written]);

	public void tUpdateAndDraw( int x, int y)  // t進行描画
	{
		if ( bActivated )
		{
			y += 0x143;

			//invariant so the decimal separator is always the '.' the console font carries. TryWrite
			//formats straight into the buffer, so redrawing every line every frame costs nothing
			CultureInfo culture = CultureInfo.InvariantCulture;
			Span<char> text = stackalloc char[64];

			text.TryWrite(culture, $"BGM/D/G/B Adj: {CDTXMania.DTX.nBGMAdjust:####0}/{CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums:####0}/{CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar:####0}/{CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass:####0} ms", out int written);
			Line(x, y, text, written);
			y -= 0x10;

			text.TryWrite(culture, $"BGMAdjCommon : {CDTXMania.ConfigIni.nCommonBGMAdjustMs:####0} ms", out written);
			Line(x, y, text, written);
			y -= 0x10;

			int num = (CDTXMania.DTX.listChip.Count > 0) ? CDTXMania.DTX.listChip[CDTXMania.DTX.listChip.Count - 1].nPlaybackTimeMs : 0;

			text.TryWrite(culture, $"Time: {CDTXMania.Timer.nCurrentTime / 1000.0:####0.000} / {num / 1000.0:####0.000}", out written);
			Line(x, y, text, written);
			y -= 0x10;

			text.TryWrite(culture, $"Part:          {n小節番号:####0}", out written);
			Line(x, y, text, written);
			y -= 0x10;

			text.TryWrite(culture, $"BPM:           {dbBPM:####0.00}", out written);
			Line(x, y, text, written);
			y -= 0x10;

			text.TryWrite(culture, $"Frame:         {CDTXMania.FPS.nCurrentFPS:####0} fps", out written);
			Line(x, y, text, written);
			y -= 0x10;

			if (AudioMixer.Device.MixesChannels)
			{
				AudioDeviceStatus audio = AudioMixer.Device.Status;

				text.TryWrite(culture, $"Sound CPU : {audio.CpuUsage:####0.00}%", out written);
				Line(x, y, text, written);
				y -= 0x10;

				text.TryWrite(culture, $"Sound Mixing:  {audio.MixedChannels:####0}", out written);
				Line(x, y, text, written);
				y -= 0x10;

				text.TryWrite(culture, $"Sound Streams: {audio.Streams:####0}", out written);
				Line(x, y, text, written);
				y -= 0x10;
			}
		}
	}
}