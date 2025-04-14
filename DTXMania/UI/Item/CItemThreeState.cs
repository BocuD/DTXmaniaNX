namespace DTXMania.UI.Item;

/// <summary>
/// 「スリーステート」（ON, OFF, 不定 の3状態）を表すアイテム。
///  Three-state (3 states: ON, OFF, or undefined) item.
/// </summary>
internal class CItemThreeState : CItemBase
{
	public EState eCurrentState;
	public enum EState
	{
		ON,
		OFF,
		UNDEFINED
	}

	public CItemThreeState()
	{
		eType = EType.ONorOFForUndefined3State;
		eCurrentState = EState.UNDEFINED;
	}
	public CItemThreeState(string strItemName, EState eInitialState, string strDescriptionJp, string strDescriptionEn)
		: this()
	{
		tInitialize(strItemName, EPanelType.Normal, strDescriptionJp, strDescriptionEn);
		eCurrentState = eInitialState;
	}
		
	// CItemBase 実装

	protected override void tEnterPressed()
	{
		tMoveItemValueToNext();
	}
	public override void tMoveItemValueToNext()
	{
		switch( eCurrentState )
		{
			case EState.ON:
				eCurrentState = EState.OFF;
				return;

			case EState.OFF:
			case EState.UNDEFINED:
				eCurrentState = EState.ON;
				return;
		}
	}
	public override void tMoveItemValueToPrevious()
	{
		switch( eCurrentState )
		{
			case EState.ON:
				eCurrentState = EState.OFF;
				return;

			case EState.OFF:
				eCurrentState = EState.ON;
				return;

			case EState.UNDEFINED:
				eCurrentState = EState.OFF;
				return;
		}
	}

	public override object GetCurrentValue()
	{
		return eCurrentState == EState.UNDEFINED ? "- -" : eCurrentState.ToString();
	}
	public override int GetIndex()
	{
		return (int)eCurrentState;
	}
	public override void SetIndex( int index )
	{
		eCurrentState = index switch
		{
			0 => EState.ON,
			1 => EState.OFF,
			2 => EState.UNDEFINED,
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}