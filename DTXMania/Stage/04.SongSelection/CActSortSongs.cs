using DTXMania.Core;
using DTXMania.UI.Item;

namespace DTXMania;

internal class CActSortSongs : CActSelectPopupMenu
{
	public CActSortSongs()
	{
		InitializeMenuItems();
	}

	private List<CItemBase> lci = [];
	private void InitializeMenuItems()
	{
		lci = [];
		
		CItemList title = new("Title", CItemBase.EPanelType.Normal, 0, "", "", ["Z,Y,X,...", "A,B,C,..."]);
		title.action = () =>
		{
			int order = title.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート2_タイトル順, eInst, order);
			actSongList.tSelectedSongHasChanged(true);
		};
		lci.Add(title);

		CItemList level = new("Level", CItemBase.EPanelType.Normal, 0, "", "", ["99,98,97,...", "1,2,3,..."]);
		level.action = () =>
		{
			int order = level.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート4_LEVEL順, eInst, order, actSongList.nTargetDifficultyLevel);
			actSongList.tSelectedSongHasChanged( true );
		};
		lci.Add(level);

		CItemList bestRank = new("Best Rank", CItemBase.EPanelType.Normal, 0, "", "", ["E,D,C,...", "SS,S,A,..."]);
		bestRank.action = () =>
		{
			int order = bestRank.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート5_BestRank順, eInst, order, actSongList.nTargetDifficultyLevel);
		};
		lci.Add(bestRank);

		CItemList playCount = new("PlayCount", CItemBase.EPanelType.Normal, 0, "", "", ["10,9,8,...", "1,2,3,..."]);
		playCount.action = () =>
		{
			int order = playCount.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート3_演奏回数の多い順, eInst, order, actSongList.nTargetDifficultyLevel);
			actSongList.tSelectedSongHasChanged( true );
		};
		lci.Add(playCount);

		CItemList artist = new("Artist", CItemBase.EPanelType.Normal, 0, "", "", ["Z,Y,X,...", "A,B,C,..."]);
		artist.action = () =>
		{
			int order = artist.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート8_アーティスト名順, eInst, order, actSongList.nTargetDifficultyLevel);
			actSongList.tSelectedSongHasChanged( true );
		};
		lci.Add(artist);

		CItemList? skillPoint = new("SkillPoint", CItemBase.EPanelType.Normal, 0, "", "", ["100,99,98,...", "1,2,3,..."]);
		skillPoint.action = () =>
		{
			int order = skillPoint.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート6_SkillPoint順, eInst, order, actSongList.nTargetDifficultyLevel);
			actSongList.tSelectedSongHasChanged( true );
		};
		lci.Add(skillPoint);

		CItemList date = new("Date", CItemBase.EPanelType.Normal, 0, "", "", ["Dec.31,30,...", "Jan.1,2,..."]);
		date.action = () =>
		{
			int order = date.GetIndex() * 2 - 1;
			actSongList.tSortSongList(CDTXMania.SongManager.t曲リストのソート7_更新日時順, eInst, order, actSongList.nTargetDifficultyLevel);
			actSongList.tSelectedSongHasChanged( true );
		};
		lci.Add(date);

		CItemList returnButton = new("Return", CItemBase.EPanelType.Normal, 0, "", "", [""])
		{
			action = tDeativatePopupMenu
		};
		lci.Add(returnButton);

		Initialize(lci, "SORT MENU", 0);
	}

	private CActSelectSongList actSongList;
	public void tActivatePopupMenu(EInstrumentPart einst, ref CActSelectSongList ca)
	{
		actSongList = ca;
		
		InitializeMenuItems();

		base.tActivatePopupMenu(einst);
	}

	public override void tPressEnterMain()  // tEnter押下Main
	{
		lci[nCurrentSelection].RunAction();
		lci[nCurrentSelection].RunAction();
	}
}