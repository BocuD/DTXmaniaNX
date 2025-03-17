namespace DTXMania
{
	/// <summary>
	/// 「スリーステート」（ON, OFF, 不定 の3状態）を表すアイテム。
	///  Three-state (3 states: ON, OFF, or undefined) item.
	/// </summary>
	internal class CItemThreeState : CItemBase
	{
		// プロパティ

		public E状態 e現在の状態;
		public enum E状態
		{
			ON,
			OFF,
			不定
		}


		// コンストラクタ

		public CItemThreeState()
		{
			eType = EType.ONorOFForUndefined3State;
			e現在の状態 = E状態.不定;
		}
		public CItemThreeState(string str項目名, E状態 e初期状態, string str説明文jp, string str説明文en)
			: this() {
			tInitialize(str項目名, e初期状態, str説明文jp, str説明文en);
		}
		
		// CItemBase 実装

		protected override void tEnter押下()
		{
			tMoveItemValueToNext();
		}
		public override void tMoveItemValueToNext()
		{
			switch( e現在の状態 )
			{
				case E状態.ON:
					e現在の状態 = E状態.OFF;
					return;

				case E状態.OFF:
					e現在の状態 = E状態.ON;
					return;

				case E状態.不定:
					e現在の状態 = E状態.ON;
					return;
			}
		}
		public override void tMoveItemValueToPrevious()
		{
			switch( e現在の状態 )
			{
				case E状態.ON:
					e現在の状態 = E状態.OFF;
					return;

				case E状態.OFF:
					e現在の状態 = E状態.ON;
					return;

				case E状態.不定:
					e現在の状態 = E状態.OFF;
					return;
			}
		}
		public void tInitialize(string str項目名, E状態 e初期状態, string str説明文jp, string str説明文en) {
			tInitialize(str項目名, e初期状態, EPanelType.Normal, str説明文jp, str説明文en);
		}
		
		public void tInitialize(string str項目名, E状態 e初期状態, EPanelType eパネル種別, string str説明文jp, string str説明文en) {
			base.tInitialize(str項目名, eパネル種別, str説明文jp, str説明文en);
			e現在の状態 = e初期状態;
		}
		public override object obj現在値()
		{
			if ( e現在の状態 == E状態.不定 )
			{
				return "- -";
			}
			else
			{
				return e現在の状態.ToString();
			}
		}
		public override int GetIndex()
		{
			return (int)e現在の状態;
		}
		public override void SetIndex( int index )
		{
		    switch (index )
		    {
		        case 0:
		            e現在の状態 = E状態.ON;
		            break;
		        case 1:
		            e現在の状態 = E状態.OFF;
		            break;
		        case 2:
		            e現在の状態 = E状態.不定;
		            break;
		        default:
		            throw new ArgumentOutOfRangeException();
		    }
		}
	}
}
