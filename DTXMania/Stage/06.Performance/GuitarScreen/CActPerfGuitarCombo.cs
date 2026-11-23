using System.Numerics;
namespace DTXMania;

internal class CActPerfGuitarCombo : CActPerfCommonCombo
{
    public CActPerfGuitarCombo(PerformanceData[] parts) : base(parts)
    {
    }

	// CActPerfCommonCombo 実装

	protected override void tDrawCombo_Guitar( Matrix4x4 origin, int nCombo値, int nジャンプインデックス )
	{
		int x = 560;
		int y = 220;

		base.tDrawCombo_Guitar(origin, nCombo値, nジャンプインデックス, x, y);
	}
	protected override void tDrawCombo_Drums( Matrix4x4 origin, int nCombo値, int nジャンプインデックス )
	{
	}
	protected override void tDrawCombo_Bass( Matrix4x4 origin, int nCombo値, int nジャンプインデックス )
	{
		int x = 560;
		int y = 220;

		base.tDrawCombo_Bass(origin, nCombo値, nジャンプインデックス, x, y);
	}
}