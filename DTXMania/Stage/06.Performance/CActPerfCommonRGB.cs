using DTXMania.Core;
using DTXMania.UI.Drawable;
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
	protected BaseTexture txRGB;

	// コンストラクタ

	public CActPerfCommonRGB()
	{
		bActivated = false;
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
		if ( bActivated )
		{
			txRGB = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_RGB buttons.png"));
			base.OnManagedCreateResources();
		}
	}
}