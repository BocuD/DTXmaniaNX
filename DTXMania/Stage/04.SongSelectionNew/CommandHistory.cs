using DTXMania.Core;

namespace DTXMania;


public class CCommandHistory // #24063 2011.1.16 yyagi コマンド入力履歴を保持_確認するクラス
{
	private struct STCommandTime // #24063 2011.1.16 yyagi コマンド入力時刻の記録用
	{
		public EInstrumentPart eInst; // 使用楽器
		public EPadFlag ePad; // 押されたコマンド(同時押しはOR演算で列挙する)
		public long time; // コマンド入力時刻
	}

	readonly int buffersize = 16;
	private List<STCommandTime> stct;

	public CCommandHistory() // コンストラクタ
	{
		stct = new List<STCommandTime>(buffersize);
	}

	/// <summary>
	/// コマンド入力履歴へのコマンド追加
	/// </summary>
	/// <param name="_eInst">楽器の種類</param>
	/// <param name="_ePad">入力コマンド(同時押しはOR演算で列挙すること)</param>
	public void Add(EInstrumentPart _eInst, EPadFlag _ePad)
	{
		STCommandTime _stct = new STCommandTime
		{
			eInst = _eInst,
			ePad = _ePad,
			time = CDTXMania.Timer.nCurrentTime
		};

		if (stct.Count >= buffersize)
		{
			stct.RemoveAt(0);
		}

		stct.Add(_stct);
//Debug.WriteLine( "CMDHIS: 楽器=" + _stct.eInst + ", CMD=" + _stct.ePad + ", time=" + _stct.time );
	}

	public void RemoveAt(int index)
	{
		stct.RemoveAt(index);
	}

	/// <summary>
	/// コマンド入力に成功しているか調べる
	/// </summary>
	/// <param name="_ePad">入力が成功したか調べたいコマンド</param>
	/// <param name="_eInst">対象楽器</param>
	/// <returns>コマンド入力成功時true</returns>
	public bool CheckCommand(EPadFlag[] _ePad, EInstrumentPart _eInst)
	{
		int targetCount = _ePad.Length;
		int stciCount = stct.Count;
		if (stciCount < targetCount)
		{
//Debug.WriteLine("NOT start checking...stciCount=" + stciCount + ", targetCount=" + targetCount);
			return false;
		}

		long curTime = CDTXMania.Timer.nCurrentTime;
//Debug.WriteLine("Start checking...targetCount=" + targetCount);
		for (int i = targetCount - 1, j = stciCount - 1; i >= 0; i--, j--)
		{
			if (_ePad[i] != stct[j].ePad)
			{
//Debug.WriteLine( "CMD解析: false targetCount=" + targetCount + ", i=" + i + ", j=" + j + ": ePad[]=" + _ePad[i] + ", stci[j] = " + stct[j].ePad );
				return false;
			}

			if (stct[j].eInst != _eInst)
			{
//Debug.WriteLine( "CMD解析: false " + i );
				return false;
			}

			if (curTime - stct[j].time > 500)
			{
//Debug.WriteLine( "CMD解析: false " + i + "; over 500ms" );
				return false;
			}

			curTime = stct[j].time;
		}

//Debug.Write( "CMD解析: 成功!(" + _ePad.Length + ") " );
//for ( int i = 0; i < _ePad.Length; i++ ) Debug.Write( _ePad[ i ] + ", " );
//Debug.WriteLine( "" );
		//stct.RemoveRange( 0, targetCount );			// #24396 2011.2.13 yyagi 
		stct.Clear(); // #24396 2011.2.13 yyagi Clear all command input history in case you succeeded inputting some command

		return true;
	}
}