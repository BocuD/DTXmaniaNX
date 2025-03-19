namespace DTXMania.UI.Item;

/// <summary>
/// 「Integer」を表すアイテム。
///	 Integer item. (with minimum and maximum values)
/// </summary>
internal class CItemInteger : CItemBase
{
	// プロパティ

	public int nCurrentValue;
	public bool b値がフォーカスされている;


	// コンストラクタ

	public CItemInteger()
	{
		eType = EType.Integer;
		n最小値 = 0;
		n最大値 = 0;
		nCurrentValue = 0;
		b値がフォーカスされている = false;
	}
	public CItemInteger(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp, string str説明文en)
		: this() {
		tInitialize(str項目名, n最小値, n最大値, n初期値, str説明文jp, str説明文en);
	}
		
	// CItemBase 実装

	protected override void tEnter押下()
	{
		b値がフォーカスされている = !b値がフォーカスされている;
	}
	public override void tMoveItemValueToNext()
	{
		if( ++nCurrentValue > n最大値 )
		{
			nCurrentValue = n最大値;
		}
	}
	public override void tMoveItemValueToPrevious()
	{
		if( --nCurrentValue < n最小値 )
		{
			nCurrentValue = n最小値;
		}
	}
	public void tInitialize(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp, string str説明文en) {
		tInitialize(str項目名, n最小値, n最大値, n初期値, EPanelType.Normal, str説明文jp, str説明文en);
	}
	public void tInitialize(string str項目名, int n最小値, int n最大値, int n初期値, EPanelType eパネル種別, string str説明文jp, string str説明文en) {
		base.tInitialize(str項目名, eパネル種別, str説明文jp, str説明文en);
		this.n最小値 = n最小値;
		this.n最大値 = n最大値;
		nCurrentValue = n初期値;
		b値がフォーカスされている = false;
	}
	public override object obj現在値()
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
	// Other

	#region [ private ]
	//-----------------
	private int n最小値;
	private int n最大値;
	//-----------------
	#endregion
}