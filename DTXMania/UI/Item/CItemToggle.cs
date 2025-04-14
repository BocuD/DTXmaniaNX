namespace DTXMania.UI.Item;

/// <summary>
/// 「トグル」（ON, OFF の2状態）を表すアイテム。
///  Toggle (2 states: ON or OFF) item.
/// </summary>
internal class CItemToggle : CItemBase
{
	public bool bON;

	public CItemToggle()
	{
		eType = EType.ONorOFFToggle;
		bON = false;
	}
	public CItemToggle(string strItemName, bool b初期状態, string strDescriptionJp, string strDescriptionEn)
		: this()
	{
		tInitialize(strItemName, EPanelType.Normal, strDescriptionJp, strDescriptionEn);
		bON = b初期状態;
	}

	// CItemBase 実装

	protected override void tEnterPressed()
	{
		tMoveItemValueToNext();
	}
	public override void tMoveItemValueToNext()
	{
		bON = !bON;
	}
	public override void tMoveItemValueToPrevious()
	{
		tMoveItemValueToNext();
	}

	public override object GetCurrentValue()
	{
		return bON ? "ON" : "OFF";
	}
	public override int GetIndex()
	{
		return bON ? 1 : 0;
	}
	
	public override void SetIndex( int index )
	{
		switch ( index )
		{
			case 0:
				bON = false;
				break;
			case 1:
				bON = true;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}