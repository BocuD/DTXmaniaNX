using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Drawable;
using FDK;

namespace DTXMania;

internal class CActPerfCommonScore : CActivity
{
	// プロパティ

	public STDGBVALUE<long> nスコアの増分;
	public STDGBVALUE<int>[] x位置 = new STDGBVALUE<int>[10];
	public STDGBVALUE<long> n現在表示中のスコア;
	public STDGBVALUE<int> n本体X;
	public int n本体Y;
	public long n進行用タイマ;
	protected BaseTexture txScore;
	private readonly PerformanceData[] parts;
	private STDGBVALUE<double> lastScore;

		
	// コンストラクタ

	public CActPerfCommonScore(PerformanceData[] parts)
	{
		this.parts = parts;
		bActivated = false;
	}


	//one 10ms step of the shown score rolling up to the real one, a twentieth of the gap at a time
	protected void RollStep( int part )
	{
		double score = parts[ part ].Score;
		if ( score != lastScore[ part ] )
		{
			lastScore[ part ] = score;
			nスコアの増分[ part ] = Math.Max( (long) ( ( score - n現在表示中のスコア[ part ] ) / 20.0 ), 1L );
		}

		n現在表示中のスコア[ part ] = Math.Min( n現在表示中のスコア[ part ] + nスコアの増分[ part ], (long) score );
	}


	protected void tDrawScore( int x, int y, long score )
	{
		if ( txScore == null )
		{
			return;
		}

		Span<char> digits = stackalloc char[ 7 ];
		for ( int i = digits.Length - 1; i >= 0; i-- )
		{
			digits[ i ] = score > 0L || i == digits.Length - 1 ? (char) ( '0' + score % 10L ) : ' ';
			score /= 10L;
		}

		for ( int i = 0; i < digits.Length; i++ )
		{
			RectangleF rectangle = digits[ i ] == ' '
				? new RectangleF( 0, 0, 0, 0 )
				: new RectangleF( ( digits[ i ] - '0' ) * 36, 0, 36, 50 );

			txScore.tDraw2D( x + i * 34, 28 + y, rectangle );
		}

		txScore.tDraw2D( x, y, new RectangleF( 0, 50, 86, 28 ) );
	}


	// CActivity 実装

	public override void OnActivate()
	{
		n進行用タイマ = -1;
		for( int i = 0; i < 3; i++ )
		{
			n現在表示中のスコア[ i ] = 0L;
			lastScore[ i ] = 0;
			nスコアの増分[ i ] = 0L;
		}
		base.OnActivate();
	}
	public override void OnManagedCreateResources()
	{
		if (bActivated)
		{
			txScore = BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\7_score numbersGD.png"));
			base.OnManagedCreateResources();
		}
	}
}