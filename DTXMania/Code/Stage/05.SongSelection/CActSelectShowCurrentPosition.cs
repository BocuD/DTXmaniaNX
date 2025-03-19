using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActSelectShowCurrentPosition : CActivity
{
	// メソッド

	public CActSelectShowCurrentPosition()
	{
		bNotActivated = true;
	}

	// CActivity 実装

	public override void OnActivate()
	{
		if ( bActivated )
			return;

		base.OnActivate();
	}
	public override void OnDeactivate()
	{
		base.OnDeactivate();
	}
	public override void OnManagedCreateResources()
	{
		if ( !bNotActivated )
		{
			txScrollBar = CDTXMania.tGenerateTexture( CSkin.Path(@"Graphics\5_scrollbar.png"), false );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if ( !bNotActivated )
		{
			CDTXMania.tDisposeSafely( ref txScrollBar );
			base.OnManagedReleaseResources();
		}
	}
	public override int OnUpdateAndDraw()
	{
		int x = 1280 - 24 + 50;
		int y = 120;

		if ( txScrollBar != null )
		{
			#region [ スクロールバーの描画 #27648 ]
			txScrollBar.tDraw2DFloat(CDTXMania.app.Device, x - (CDTXMania.stageSongSelection.ct登場時アニメ用共通.nCurrentValue / 2f), y, new Rectangle(0, 0, 12, 492));	// 本当のy座標は88なんだが、なぜか約30のバイアスが掛かる___
			#endregion
			#region [ スクロール地点の描画 (計算はCActSelect曲リストで行う。スクロール位置と選曲項目の同期のため。)#27648 ]
			int py = CDTXMania.stageSongSelection.nScrollbarRelativeYCoordinate;
			if ( py <= 492 - 12 && py >= 0 )
			{
				txScrollBar.tDraw2DFloat(CDTXMania.app.Device, x - (CDTXMania.stageSongSelection.ct登場時アニメ用共通.nCurrentValue / 2f), y + py, new Rectangle(0, 492, 12, 12));
			}
			#endregion
		}

		return 0;
	}


	// Other

	#region [ private ]
	//-----------------
	private CTexture txScrollBar;
	//-----------------
	#endregion
}