using System.Numerics;
using DTXMania.Core;

namespace DTXMania;

internal class CActPerfDrumsScore : CActPerfCommonScore
{
    public CActPerfDrumsScore(PerformanceData[] parts) : base(parts)
    {
    }

    // CActivity 実装（共通クラスからの差分のみ）

    public override void OnActivate()
    {
        n本体X[0] = 40;
        n本体Y = 13;

        base.OnActivate();
    }

    public override void Update()
    {
        if (bActivated)
        {
            if (bJustStartedUpdate)
            {
                n進行用タイマ = CDTXMania.Timer.nCurrentTime;
                bJustStartedUpdate = false;
            }
            if (CDTXMania.stagePerfDrumsScreen.bIsTrainingMode)
            {
                n現在表示中のスコア[0] = 0;
            }
            else
            {
                long num = CDTXMania.Timer.nCurrentTime;
                if (num < n進行用タイマ)
                {
                    n進行用タイマ = num;
                }
                while ((num - n進行用タイマ) >= 10)
                {
                    RollStep(0);
                    n進行用タイマ += 10;
                }
            }
        }
    }

    public override void Draw(Matrix4x4 origin, EInstrumentPart part)
    {
        if (bActivated)
        {
            tDrawScore(origin, n本体X[0], n本体Y, n現在表示中のスコア[0]);
        }
    }
    #region [ private ]
    //-----------------
    //-----------------
    #endregion
}