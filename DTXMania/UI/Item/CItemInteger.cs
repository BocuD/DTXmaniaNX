namespace DTXMania.UI.Item;

/// <summary>
/// 「Integer」を表すアイテム。
///	 Integer item. (with minimum and maximum values)
/// </summary>
internal class CItemInteger : CItemBase
{
	public int nCurrentValue;
	public bool bIsCurrentlyInFocus;
	
	private readonly int nMinimumValue;
	private readonly int nMaximumValue;
	
	public CItemInteger(string strItemName, int nMinimumValue, int nMaximumValue, int nCurrentValue, string strDescriptionJp, string strDescriptionEn)
	{
		tInitialize(strItemName, EPanelType.Normal, strDescriptionJp, strDescriptionEn);

		eType = EType.Integer;
		
		this.nMinimumValue = nMinimumValue;
		this.nMaximumValue = nMaximumValue;
		this.nCurrentValue = nCurrentValue;
		
		bIsCurrentlyInFocus = false;
	}
		
	// CItemBase 実装

	protected override void tEnterPressed()
	{
		bIsCurrentlyInFocus = !bIsCurrentlyInFocus;
	}
	public override void tMoveItemValueToNext()
	{
		if( ++nCurrentValue > nMaximumValue )
		{
			nCurrentValue = nMaximumValue;
		}
	}
	public override void tMoveItemValueToPrevious()
	{
		if( --nCurrentValue < nMinimumValue )
		{
			nCurrentValue = nMinimumValue;
		}
	}

	public override object GetCurrentValue()
	{
		return nCurrentValue;
	}
	public override int GetIndex()
	{
		return nCurrentValue;
	}
	public override void SetIndex( int index )
	{
		nCurrentValue = index;
	}
}