using Rectangle = System.Drawing.Rectangle;

namespace DTXMania;

internal class CActPerfDrumsScore : CActPerfCommonScore
{
    // CActivity 実装（共通クラスからの差分のみ）

    public override void OnActivate()
    {
        n本体X[0] = 40;
        n本体Y = 13;

        base.OnActivate();
    }

    public override unsafe int OnUpdateAndDraw()
    {
        if (!bNotActivated && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
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
                    n現在表示中のスコア[0] += nスコアの増分[0];

                    if (n現在表示中のスコア[0] > (long)nCurrentTrueScore[0])
                        n現在表示中のスコア[0] = (long)nCurrentTrueScore[0];
                    n進行用タイマ += 10;
                }
            }
            string str = string.Format("{0,7:######0}", n現在表示中のスコア[0]);
            for (int i = 0; i < 7; i++)
            {
                Rectangle rectangle;
                char ch = str[i];
                if (ch.Equals(' '))
                {
                    rectangle = new Rectangle(0, 0, 0, 0);
                }
                else
                {
                    int num4 = int.Parse(str.Substring(i, 1));
                    rectangle = new Rectangle(num4 * 36, 0, 36, 50);
                }
                if( txScore != null )
                {
                    txScore.tDraw2D(CDTXMania.app.Device, n本体X[0] + (i * 34), 28 + n本体Y, rectangle);
                }
            }
            if( txScore != null )
            {
                txScore.tDraw2D(CDTXMania.app.Device, n本体X[0], n本体Y, new Rectangle(0, 50, 86, 28));
            }
        }
        return 0;
    }
    #region [ private ]
    //-----------------
    //-----------------
    #endregion
}