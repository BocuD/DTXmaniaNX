using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActFIFOBlackStart : CActivity
{
	// メソッド

	public void tStartFadeOut()
	{
		mode = EFIFOMode.FadeOut;  // tフェードアウト開始
		counter = new CCounter( 0, 150, 5, CDTXMania.Timer );
	}
	public void tStartFadeIn()  // tフェードイン開始
	{
		mode = EFIFOMode.FadeIn;
		counter = new CCounter( 0, 150, 5, CDTXMania.Timer );
	}

		
	// CActivity 実装

	public override void OnDeactivate()
	{
		if( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref tx黒タイル64x64 );
			CDTXMania.tReleaseTexture( ref tx黒幕 );
			CDTXMania.tReleaseTexture( ref txジャケット );
			base.OnDeactivate();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			tx黒タイル64x64 = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\Tile black 64x64.png" ), false );
			tx黒幕 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\6_FadeOut.jpg"), false);
			base.OnManagedCreateResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		if( bNotActivated || ( counter == null ) )
		{
			return 0;
		}
		counter.tUpdate();
		// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
		if (tx黒幕 != null)
		{
			tx黒幕.nTransparency = (mode == EFIFOMode.FadeIn) ? (((100 - counter.nCurrentValue) * 0xff) / 100) : ((counter.nCurrentValue * 0xff) / 100);
			tx黒幕.tDraw2D(CDTXMania.app.Device, 0, 0);
			string path = CDTXMania.DTX.strFolderName + CDTXMania.DTX.PREIMAGE;
			if( txジャケット == null ) // 2019.04.26 kairera0467
			{
				if (!File.Exists(path))
				{
					//Trace.TraceWarning("ファイルが存在しません。({0})", new object[] { path });
					txジャケット = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\\5_preimage default.png"));
				}
				else
				{
					txジャケット = CDTXMania.tGenerateTexture(path);
				}
			}

			if( txジャケット != null )
			{
				txジャケット.vcScaleRatio.X = 0.96f;
				txジャケット.vcScaleRatio.Y = 0.96f;
				txジャケット.fZAxisRotation = 0.28f;
				txジャケット.nTransparency = (mode == EFIFOMode.FadeIn) ? (((100 - counter.nCurrentValue) * 0xff) / 100) : ((counter.nCurrentValue * 0xff) / 100);
				txジャケット.tDraw2D(CDTXMania.app.Device, 620, 40);
			}
		}
		else if (tx黒幕 == null)
		{
			tx黒タイル64x64.nTransparency = (mode == EFIFOMode.FadeIn) ? (((100 - counter.nCurrentValue) * 0xff) / 100) : ((counter.nCurrentValue * 0xff) / 100);
			for (int i = 0; i <= (SampleFramework.GameFramebufferSize.Width / 64); i++)		// #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
			{
				for (int j = 0; j <= (SampleFramework.GameFramebufferSize.Height / 64); j++)	// #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
				{
					tx黒タイル64x64.tDraw2D(CDTXMania.app.Device, i * 64, j * 64);
				}
			}
		}
		if( counter.nCurrentValue != 150 )
		{
			return 0;
		}
		return 1;
	}


	// Other

	#region [ private ]
	//-----------------
	public CCounter counter;
	private EFIFOMode mode;
	private CTexture tx黒タイル64x64;
	private CTexture tx黒幕;
	private CTexture txジャケット;
	//-----------------
	#endregion
}