using DTXMania.Core;
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

	public void tUpdateAndDraw( int x, int y)  // t進行描画
	{
		if( bActivated )
		{
			y += 0x143;
			CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("BGM/D/G/B Adj: {0:####0}/{1:####0}/{2:####0}/{3:####0} ms", CDTXMania.DTX.nBGMAdjust, CDTXMania.ConfigIni.nInputAdjustTimeMs.Drums, CDTXMania.ConfigIni.nInputAdjustTimeMs.Guitar, CDTXMania.ConfigIni.nInputAdjustTimeMs.Bass));
			y -= 0x10;
			CDTXMania.actDisplayString.tPrint( x, y, CCharacterConsole.EFontType.White, string.Format( "BGMAdjCommon : {0:####0} ms", CDTXMania.ConfigIni.nCommonBGMAdjustMs ) );
			y -= 0x10;
			int num = (CDTXMania.DTX.listChip.Count > 0) ? CDTXMania.DTX.listChip[CDTXMania.DTX.listChip.Count - 1].nPlaybackTimeMs : 0;
			string str = "Time: " + ((((double)CDTXMania.Timer.nCurrentTime) / 1000.0)).ToString("####0.000") + " / " + ((((double)num) / 1000.0)).ToString("####0.000");
			CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, str);
			y -= 0x10;
			CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("Part:          {0:####0}", n小節番号));
			y -= 0x10;
			CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("BPM:           {0:####0.00}", dbBPM));
			y -= 0x10;
			CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("Frame:         {0:####0} fps", CDTXMania.FPS.nCurrentFPS));
			y -= 0x10;
                    
			if (CDTXMania.ConfigIni.nSoundDriverType != 0)
			{
				CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("Sound CPU : {0:####0.00}%", CDTXMania.SoundManager.GetCPUusage()));
				y -= 0x10;
				CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("Sound Mixing:  {0:####0}", CDTXMania.SoundManager.GetMixingStreams()));
				y -= 0x10;
				CDTXMania.actDisplayString.tPrint(x, y, CCharacterConsole.EFontType.White, string.Format("Sound Streams: {0:####0}", CDTXMania.SoundManager.GetStreams()));
				y -= 0x10;
			}
		}
	}
}