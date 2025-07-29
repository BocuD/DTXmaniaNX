using DTXMania.Core;
using FDK;
using SharpDX;
using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActScrollBar : CActivity
{
	CStageSongSelection stageSongSelection;

	public CActScrollBar(CStageSongSelection cStageSongSelection)
	{
		stageSongSelection = cStageSongSelection;
		bActivated = false;
	}

	// CActivity 実装

	public override void OnManagedCreateResources()
	{
		if ( bActivated )
		{
			txScrollBar = CDTXMania.tGenerateTexture( CSkin.Path(@"Graphics\5_scrollbar.png"), false );
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if ( bActivated )
		{
			CDTXMania.tReleaseTexture(ref txScrollBar);
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
			txScrollBar.tDraw2DFloat(CDTXMania.app.Device, x - (stageSongSelection.ctInitialAppearAnimation.nCurrentValue / 2f), y, new RectangleF(0, 0, 12, 492));	// 本当のy座標は88なんだが、なぜか約30のバイアスが掛かる___
			#endregion
			#region [ スクロール地点の描画 (計算はCActSelect曲リストで行う。スクロール位置と選曲項目の同期のため。)#27648 ]
			int py = stageSongSelection.nScrollbarRelativeYCoordinate;
			if ( py <= 492 - 12 && py >= 0 )
			{
				txScrollBar.tDraw2DFloat(CDTXMania.app.Device, x - (stageSongSelection.ctInitialAppearAnimation.nCurrentValue / 2f), y + py, new RectangleF(0, 492, 12, 12));
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