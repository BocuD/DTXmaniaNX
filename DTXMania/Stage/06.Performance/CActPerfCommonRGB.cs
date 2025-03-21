using DTXMania.Core;
using FDK;

namespace DTXMania;

internal class CActPerfCommonRGB : CActivity
{
	//こっちではほとんどやることなんてないんだけどね____
	//一応暫定対応として押している状態を取得&発信しているだけ。

	// プロパティ

	public bool[] bPressedState = new bool[ 10 ];
	protected STDGBVALUE<int> nシャッター上;
	protected STDGBVALUE<int> nシャッター下;
	protected STDGBVALUE<double> dbAboveShutter;
	protected STDGBVALUE<double> dbUnderShutter;
	protected double db倍率 = 6.14;
	protected CTexture txRGB;
	protected CTexture txShutter;
	protected CActLVLNFont actLVFont;

	// コンストラクタ

	public CActPerfCommonRGB()
	{
		listChildActivities.Add(actLVFont = new CActLVLNFont());
		bNotActivated = true;
	}
		
		
	// メソッド

	public void Push( int nLane )
	{
		bPressedState[ nLane ] = true;
	}


	// CActivity 実装

	public override void OnActivate()
	{
		for( int i = 0; i < 10; i++ )
		{
			bPressedState[ i ] = false;
		}
		base.OnActivate();
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			txRGB = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_RGB buttons.png"));
			txShutter = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\7_shutter_GB.png"));
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if( !bNotActivated )
		{
			CDTXMania.tReleaseTexture( ref txRGB );
			CDTXMania.tReleaseTexture(ref txShutter);
			base.OnManagedReleaseResources();
		}
	}
}