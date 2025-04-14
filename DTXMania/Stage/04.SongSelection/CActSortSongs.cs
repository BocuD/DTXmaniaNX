using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal class CActSortSongs : CActSelectPopupMenu
{

	// コンストラクタ

	public CActSortSongs()
	{
		InitializeMenuItems();
		
		tActivatePopupMenu(EInstrumentPart.DRUMS);
	}

	private void InitializeMenuItems()
	{
		List<CItemBase> lci = new();
		lci.Add(new CItemList("Title", CItemBase.EPanelType.Normal, 0, "", "", "Z,Y,X,...", "A,B,C,...")
		{
			action = 
		});
		lci.Add(new CItemList("Level", CItemBase.EPanelType.Normal, 0, "", "", "99,98,97,...", "1,2,3,..."));
		lci.Add(new CItemList("Best Rank", CItemBase.EPanelType.Normal, 0, "", "", "E,D,C,...", "SS,S,A,..."));
		lci.Add(new CItemList("PlayCount", CItemBase.EPanelType.Normal, 0, "", "", "10,9,8,...", "1,2,3,..."));
		lci.Add(new CItemList("Author", CItemBase.EPanelType.Normal, 0, "", "", "Z,Y,X,...", "A,B,C,..."));
		lci.Add(new CItemList("SkillPoint", CItemBase.EPanelType.Normal, 0, "", "", "100,99,98,...", "1,2,3,..."));
		lci.Add(new CItemList("Date", CItemBase.EPanelType.Normal, 0, "", "", "Dec.31,30,...", "Jan.1,2,..."));
		lci.Add(new CItemList("Return", CItemBase.EPanelType.Normal, 0, "", "", "", ""));

		Initialize(lci, "SORT MENU", 0);
	}

	// メソッド
	public void tActivatePopupMenu(EInstrumentPart einst, ref CActSelectSongList ca)
	{
		actSongList = ca;
		
		InitializeMenuItems();

		base.tActivatePopupMenu(einst);
	}

	public override void tPressEnterMain( int nSortOrder)  // tEnter押下Main
	{
		nSortOrder *= 2;	// 0,1  => -1, 1
		nSortOrder -= 1;
		
		switch ( nCurrentSelection )
		{
			case (int) EOrder.Title:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート2_タイトル順, eInst, nSortOrder
				);
				actSongList.tSelectedSongHasChanged(true);
				break;
			case (int) EOrder.Level:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート4_LEVEL順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				actSongList.tSelectedSongHasChanged( true );
				break;
			case (int) EOrder.BestRank:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート5_BestRank順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				break;
			case (int) EOrder.PlayCount:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート3_演奏回数の多い順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				actSongList.tSelectedSongHasChanged( true );
				break;
			case (int) EOrder.Author:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート8_アーティスト名順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				actSongList.tSelectedSongHasChanged( true );
				break;
			case (int) EOrder.SkillPoint:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート6_SkillPoint順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				actSongList.tSelectedSongHasChanged( true );
				break;
			case (int) EOrder.Date:
				actSongList.tSortSongList(
					CDTXMania.SongManager.t曲リストのソート7_更新日時順, eInst, nSortOrder,
					actSongList.n現在のアンカ難易度レベル
				);
				actSongList.tSelectedSongHasChanged( true );
				break;
			case (int) EOrder.Return:
				tDeativatePopupMenu();
				break;
			default:
				break;
		}
	}
		
	// CActivity 実装

	public override void OnDeactivate()
	{
		if( !bNotActivated )
		{
			base.OnDeactivate();
		}
	}
	public override void OnManagedCreateResources()
	{
		if( !bNotActivated )
		{
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		base.OnManagedReleaseResources();
	}

	#region [ private ]
	//-----------------

	private CActSelectSongList actSongList;  // act曲リスト

	private enum EOrder : int
	{
		Title = 0, 
		Level, 
		BestRank, 
		PlayCount,
		Author,
		SkillPoint,
		Date,
		Return, END,
		Default = 99
	};
	//-----------------
	#endregion
}