namespace DTXMania.UI.Item;

/// <summary>
/// 「List」（複数の固定値からの１つを選択可能）を表すアイテム。
///  List item (select one from multiple fixed values).
/// </summary>
internal class CItemList : CItemBase
{
	public List<string> listItemValues;
	public int nCurrentlySelectedIndex;


	// コンストラクタ

	public CItemList()
	{
		eType = EType.List;
		nCurrentlySelectedIndex = 0;
		listItemValues = [];
	}
	public CItemList(string strItemName, EPanelType panelType, int initialSelectedIndex, string strDescriptionJp, string strDescriptionEn, string[] argItemList)
		: this() {
		tInitialize(strItemName, panelType, initialSelectedIndex, strDescriptionJp, strDescriptionEn, argItemList);
	}


	// CItemBase 実装

	protected override void tEnterPressed()
	{
		tMoveItemValueToNext();
	}
	public override void tMoveItemValueToNext()
	{
		if( ++nCurrentlySelectedIndex >= listItemValues.Count )
		{
			nCurrentlySelectedIndex = 0;
		}
	}
	public override void tMoveItemValueToPrevious()
	{
		if( --nCurrentlySelectedIndex < 0 )
		{
			nCurrentlySelectedIndex = listItemValues.Count - 1;
		}
	}

	public void tInitialize(string strItemDescription, EPanelType ePanelType, int nInitialSelectedIndex, string strDescriptionJp, string strDescriptionEn, params string[] arg項目リスト) {
		base.tInitialize(strItemDescription, ePanelType, strDescriptionJp, strDescriptionEn);
		nCurrentlySelectedIndex = nInitialSelectedIndex;
		foreach (string str in arg項目リスト) {
			listItemValues.Add(str);
		}
	}

	public override object GetCurrentValue()
	{
		return listItemValues[nCurrentlySelectedIndex];
	}

	public override int GetIndex()
	{
		return nCurrentlySelectedIndex;
	}

	public override void SetIndex(int index)
	{
		nCurrentlySelectedIndex = index;
	}
}




/// <summary>
/// 簡易コンフィグの「切り替え」に使用する、「List」（複数の固定値からの１つを選択可能）を表すアイテム。
/// e種別が違うのと、tPressEnter()で何もしない以外は、「List」そのまま。
/// </summary>
internal class CSwitchItemList : CItemList
{
	// コンストラクタ

	public CSwitchItemList()
	{
		eType = EType.切替リスト;
		nCurrentlySelectedIndex = 0;
		listItemValues = new List<string>();
	}
	public CSwitchItemList( string strItemName, EPanelType ePanelType, int nInitialSelectedIndex, string strDescriptionJp, string strDescriptionEn, string[] argItemList )
		: this()
	{
		tInitialize( strItemName, ePanelType, nInitialSelectedIndex, strDescriptionJp, strDescriptionEn, argItemList );
	}

	protected override void tEnterPressed()
	{
		// this.tMoveItemValueToNext();	// DoNothing
	}
}