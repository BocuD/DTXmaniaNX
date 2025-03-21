namespace DTXMania.UI.Item;

/// <summary>
/// 「トグル」（ON, OFF の2状態）を表すアイテム。
///  Toggle (2 states: ON or OFF) item.
/// </summary>
internal class CItemToggle : CItemBase
{
	// プロパティ

	public bool bON;

		
	// コンストラクタ

	public CItemToggle()
	{
		eType = EType.ONorOFFToggle;
		bON = false;
	}
	public CItemToggle(string str項目名, bool b初期状態, string str説明文jp, string str説明文en)
		: this() {
		tInitialize(str項目名, b初期状態, str説明文jp, str説明文en);
	}

	// CItemBase 実装

	protected override void tEnter押下()
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
	public void tInitialize(string str項目名, bool b初期状態, string str説明文jp, string str説明文en) {
		tInitialize(str項目名, b初期状態, EPanelType.Normal, str説明文jp, str説明文en);
	}
	public void tInitialize(string str項目名, bool b初期状態, EPanelType eパネル種別, string str説明文jp, string str説明文en) {
		base.tInitialize(str項目名, eパネル種別, str説明文jp, str説明文en);
		bON = b初期状態;
	}
	public override object obj現在値()
	{
		return ( bON ) ? "ON" : "OFF";
	}
	public override int GetIndex()
	{
		return ( bON ) ? 1 : 0;
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