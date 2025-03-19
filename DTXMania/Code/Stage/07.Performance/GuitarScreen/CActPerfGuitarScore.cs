using System.Drawing;

namespace DTXMania;

internal class CActPerfGuitarScore : CActPerfCommonScore
{
    // コンストラクタ

    public CActPerfGuitarScore()
    {
        bNotActivated = true;
    }

    public override void OnActivate()
    {

        #region [ 本体位置 ]

        {
            n本体X[1] = 373;
            n本体X[2] = 665;

            n本体Y = 12;
        }

        if (CDTXMania.ConfigIni.bGraph有効.Guitar || CDTXMania.ConfigIni.bGraph有効.Bass )
        {
            if (!CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay && CDTXMania.ConfigIni.bAllBassAreAutoPlay)
            {
                n本体X[2] = 0;
            }
            else if (CDTXMania.ConfigIni.bAllGuitarsAreAutoPlay && !CDTXMania.ConfigIni.bAllBassAreAutoPlay)
            {
                n本体X[1] = 0;
            }
        }

        #endregion

        base.OnActivate();
    }

    // CActivity 実装（共通クラスからの差分のみ）

    public override unsafe int OnUpdateAndDraw()
    {
        if( !bNotActivated && !CDTXMania.DTXVmode.Enabled && !CDTXMania.DTX2WAVmode.Enabled)
        {
            if( bJustStartedUpdate )
            {
                n進行用タイマ = CDTXMania.Timer.nCurrentTime;
                bJustStartedUpdate = false;
            }
            if (CDTXMania.stagePerfGuitarScreen.bIsTrainingMode)
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
                    for (int j = 1; j < 3; j++)
                    {
                        n現在表示中のスコア[j] += nスコアの増分[j];

                        if (n現在表示中のスコア[j] > (long)nCurrentTrueScore[j])
                            n現在表示中のスコア[j] = (long)nCurrentTrueScore[j];
                    }
                    n進行用タイマ += 10;
                }
            }
            for( int j = 1; j < 3; j++ )
            {
                if ( CDTXMania.DTX.bHasChips[j] && n本体X[j] != 0 )
                {
                    string str = string.Format("{0,7:######0}", n現在表示中のスコア[j]);
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
                        if (txScore != null)
                        {
                            txScore.tDraw2D(CDTXMania.app.Device, n本体X[j] + (i * 34), 28 + n本体Y, rectangle);
                        }
                    }
                    if (txScore != null)
                    {
                        txScore.tDraw2D(CDTXMania.app.Device, n本体X[j], n本体Y, new Rectangle(0, 50, 86, 28));
                    }
                }
            }
        }
        return 0;
    }

    // Other

    #region [ private ]
    //-----------------
    //-----------------
    #endregion
}