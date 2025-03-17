using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace DTXMania
{
	internal class CActPerfDrumsJudgementString : CActPerfCommonJudgementString
	{
		// コンストラクタ

        public CActPerfDrumsJudgementString()
        {
            bNotActivated = true;
        }
		

		// CActivity 実装（共通クラスからの差分のみ）

        public override int OnUpdateAndDraw()
        {
            if (!bNotActivated && CDTXMania.ConfigIni.bDisplayJudge.Drums)
            {
                int index = 0;
                #region[ 座標など定義 ]
                if( CDTXMania.ConfigIni.nJudgeAnimeType == 1 )
                {
                    #region[ コマ方式 ]
                    for (int i = 0; i < 12; i++)
                    {
                        if (!st状態[i].ct進行.bStopped)
                        {
                            st状態[i].ct進行.tUpdate();
                            if (st状態[i].ct進行.bReachedEndValue)
                            {
                                st状態[i].ct進行.tStop();
                            }
                            st状態[i].nRect = st状態[i].ct進行.nCurrentValue;
                        }
                        index++;
                    }
                    #endregion
                }
                else if( CDTXMania.ConfigIni.nJudgeAnimeType == 2 )
                {
                    #region[ 新しいやつ ]
                    for (int i = 0; i < 12; i++)
                    {
                        if (!st状態[i].ct進行.bStopped)
                        {
                            st状態[i].ct進行.tUpdate();
                            if (st状態[i].ct進行.bReachedEndValue)
                            {
                                st状態[i].ct進行.tStop();
                            }
                            //int num2 = base.st状態[i].ctUpdate.nCurrentValue;
                            int nNowFrame = st状態[ i ].ct進行.nCurrentValue;

                            //テンプレのようなもの。
                            //拡大処理を先に行わないとめちゃくちゃになる。
                            /*
                            base.st状態[i].fX方向拡大率 = 1.0f;
                            base.st状態[i].fY方向拡大率 = 1.0f;
                            base.st状態[i].n相対X座標 = 0;
                            base.st状態[i].n相対Y座標 = 0;
                            base.st状態[i].nTransparency = 0;
                            */

                            //base.st状態[i].judge = EJudgement.Perfect;
                            //nNowFrame = 16;
                            if( st状態[ i ].judge == EJudgement.Perfect )
                            {
                                #region[ PERFECT ]
                                #region[ 0～10 ]
                                if( nNowFrame == 0 )
                                {
                                    st状態[i].fX方向拡大率 = 1.67f;
                                    st状態[i].fY方向拡大率 = 1.67f;

                                    st状態[i].fZ軸回転度 = 0;
                                    //base.st状態[i].fX方向拡大率 = 1f;
                                    //base.st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 28;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0;
                                    
                                    st状態[i].fX方向拡大率_棒 = 0f;
                                    st状態[i].fY方向拡大率_棒 = 0f;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian( -43f );
                                }
                                else if( nNowFrame == 1 )
                                {
                                    st状態[i].fX方向拡大率 = 1.33f;
                                    st状態[i].fY方向拡大率 = 1.33f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 7f );
                                    st状態[i].n相対X座標 = 26;
                                    st状態[i].n相対Y座標 = 4;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.63f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -98;
                                    st状態[i].n相対Y座標_棒 = 6;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian( -43f );
                                }
                                else if( nNowFrame == 2 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1f;
                                    st状態[i].fY方向拡大率B = 1f;
                                    st状態[i].n相対X座標B = -2;
                                    st状態[i].n相対Y座標B = 2;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(-14.5f);
                                }
                                else if( nNowFrame == 3 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.1f;
                                    st状態[i].fY方向拡大率B = 1.1f;
                                    st状態[i].n相対X座標B = -3;
                                    st状態[i].n相対Y座標B = 1;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(15f);
                                }
                                else if( nNowFrame == 4 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.2f;
                                    st状態[i].fY方向拡大率B = 1.2f;
                                    st状態[i].n相対X座標B = -4;
                                    st状態[i].n相対Y座標B = 0;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(18.5f);
                                }
                                else if( nNowFrame == 5 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.25f;
                                    st状態[i].fY方向拡大率B = 1.25f;
                                    st状態[i].n相対X座標B = -5;
                                    st状態[i].n相対Y座標B = -1;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(20.5f);
                                }
                                else if( nNowFrame == 6 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.3f;
                                    st状態[i].fY方向拡大率B = 1.3f;
                                    st状態[i].n相対X座標B = -6;
                                    st状態[i].n相対Y座標B = -2;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(20.5f);
                                }
                                else if( nNowFrame == 7 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.35f;
                                    st状態[i].fY方向拡大率B = 1.35f;
                                    st状態[i].n相対X座標B = -7;
                                    st状態[i].n相対Y座標B = -3;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -39;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(22f);
                                }
                                else if( nNowFrame == 8 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.4f;
                                    st状態[i].fY方向拡大率B = 1.4f;
                                    st状態[i].n相対X座標B = -8;
                                    st状態[i].n相対Y座標B = -4;
                                    st状態[i].n透明度B = 127;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(23.5f);
                                }
                                else if( nNowFrame == 9 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.45f;
                                    st状態[i].fY方向拡大率B = 1.45f;
                                    st状態[i].n相対X座標B = -9;
                                    st状態[i].n相対Y座標B = -5;
                                    st状態[i].n透明度B = 112;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(25.5f);
                                }
                                else if( nNowFrame == 10 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;


                                    st状態[i].fX方向拡大率B = 1.5f;
                                    st状態[i].fY方向拡大率B = 1.5f;
                                    st状態[i].n相対X座標B = -10;
                                    st状態[i].n相対Y座標B = -6;
                                    st状態[i].n透明度B = 100;


                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(27f);
                                }
                                #endregion
                                #region[ 11～18 ]
                                else if( nNowFrame == 11 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.55f;
                                    st状態[i].fY方向拡大率B = 1.55f;
                                    st状態[i].n相対X座標B = -11;
                                    st状態[i].n相対Y座標B = -7;
                                    st状態[i].n透明度B = 70;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(29.5f);
                                }
                                else if( nNowFrame == 12 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.6f;
                                    st状態[i].fY方向拡大率B = 1.6f;
                                    st状態[i].n相対X座標B = -12;
                                    st状態[i].n相対Y座標B = -8;
                                    st状態[i].n透明度B = 40;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(31f);
                                }
                                else if( nNowFrame == 13 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.65f;
                                    st状態[i].fY方向拡大率B = 1.65f;
                                    st状態[i].n相対X座標B = -13;
                                    st状態[i].n相対Y座標B = -9;
                                    st状態[i].n透明度B = 40;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(32.5f);
                                }
                                else if( nNowFrame == 14 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1.7f;
                                    st状態[i].fY方向拡大率B = 1.7f;
                                    st状態[i].n相対X座標B = -14;
                                    st状態[i].n相対Y座標B = -10;
                                    st状態[i].n透明度B = 20;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(34f);
                                }
                                else if( nNowFrame == 15 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率B = 1f;
                                    st状態[i].fY方向拡大率B = 1f;
                                    st状態[i].n相対X座標B = -14;
                                    st状態[i].n相対Y座標B = -10;
                                    st状態[i].n透明度B = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(36f);
                                }
                                else if( nNowFrame == 16 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(38f);
                                }
                                else if( nNowFrame == 17 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -46;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(40.5f);
                                }
                                else if( nNowFrame == 18 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -46;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                #endregion
                                #region[ 19～23 ]
                                else if( nNowFrame == 19 )
                                {
                                    st状態[i].fX方向拡大率 = 1.22f;
                                    st状態[i].fY方向拡大率 = 0.77f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = 16;
                                    st状態[i].n相対Y座標 = -2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.1f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -55;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if( nNowFrame == 20 )
                                {
                                    st状態[i].fX方向拡大率 = 1.45f;
                                    st状態[i].fY方向拡大率 = 0.64f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = 36;
                                    st状態[i].n相対Y座標 = -6;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.9f;
                                    st状態[i].fY方向拡大率_棒 = 0.7f;
                                    st状態[i].n相対X座標_棒 = -70;
                                    st状態[i].n相対Y座標_棒 = 4;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if( nNowFrame == 21 )
                                {
                                    st状態[i].fX方向拡大率 = 1.70f;
                                    st状態[i].fY方向拡大率 = 0.41f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = 57;
                                    st状態[i].n相対Y座標 = -9;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.6f;
                                    st状態[i].fY方向拡大率_棒 = 0.45f;
                                    st状態[i].n相対X座標_棒 = -98;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if( nNowFrame == 22 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = 75;
                                    st状態[i].n相対Y座標 = -12;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.4f;
                                    st状態[i].fY方向拡大率_棒 = 0.25f;
                                    st状態[i].n相対X座標_棒 = -120;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if( nNowFrame == 23 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( 15f );
                                    st状態[i].n相対X座標 = 75;
                                    st状態[i].n相対Y座標 = -12;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0f;
                                    st状態[i].fY方向拡大率_棒 = 0f;
                                    st状態[i].n相対X座標_棒 = -120;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                #endregion
                                #endregion
                            }
                            else if( st状態[ i ].judge == EJudgement.Great )
                            {
                                #region[ GREAT ]
                                #region[ 0～10 ]
                                if (nNowFrame == 0)
                                {
                                    st状態[i].fX方向拡大率 = 1.67f;
                                    st状態[i].fY方向拡大率 = 1.67f;

                                    st状態[i].fZ軸回転度 = 0;
                                    //base.st状態[i].fX方向拡大率 = 1f;
                                    //base.st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 28;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0f;
                                    st状態[i].fY方向拡大率_棒 = 0f;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(-43f);
                                }
                                else if (nNowFrame == 1)
                                {
                                    st状態[i].fX方向拡大率 = 1.33f;
                                    st状態[i].fY方向拡大率 = 1.33f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(7f);
                                    st状態[i].n相対X座標 = 26;
                                    st状態[i].n相対Y座標 = 4;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.63f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -98;
                                    st状態[i].n相対Y座標_棒 = 6;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(-43f);
                                }
                                else if (nNowFrame == 2)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(-14.5f);
                                }
                                else if (nNowFrame == 3)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(15f);
                                }
                                else if (nNowFrame == 4)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(18.5f);
                                }
                                else if (nNowFrame == 5)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(20.5f);
                                }
                                else if (nNowFrame == 6)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(20.5f);
                                }
                                else if (nNowFrame == 7)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -39;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(22f);
                                }
                                else if (nNowFrame == 8)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(23.5f);
                                }
                                else if (nNowFrame == 9)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(25.5f);
                                }
                                else if (nNowFrame == 10)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(27f);
                                }
                                #endregion
                                #region[ 11～18 ]
                                else if (nNowFrame == 11)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -40;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(29.5f);
                                }
                                else if (nNowFrame == 12)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(31f);
                                }
                                else if (nNowFrame == 13)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(32.5f);
                                }
                                else if (nNowFrame == 14)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(34f);
                                }
                                else if (nNowFrame == 15)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(36f);
                                }
                                else if (nNowFrame == 16)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -38;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(38f);
                                }
                                else if (nNowFrame == 17)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -46;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(40.5f);
                                }
                                else if (nNowFrame == 18)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = -2;
                                    st状態[i].n相対Y座標 = 2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.25f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -46;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                #endregion
                                #region[ 19～23 ]
                                else if (nNowFrame == 19)
                                {
                                    st状態[i].fX方向拡大率 = 1.22f;
                                    st状態[i].fY方向拡大率 = 0.77f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = 16;
                                    st状態[i].n相対Y座標 = -2;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 1.1f;
                                    st状態[i].fY方向拡大率_棒 = 1f;
                                    st状態[i].n相対X座標_棒 = -55;
                                    st状態[i].n相対Y座標_棒 = 10;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if (nNowFrame == 20)
                                {
                                    st状態[i].fX方向拡大率 = 1.45f;
                                    st状態[i].fY方向拡大率 = 0.64f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = 36;
                                    st状態[i].n相対Y座標 = -6;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.9f;
                                    st状態[i].fY方向拡大率_棒 = 0.7f;
                                    st状態[i].n相対X座標_棒 = -70;
                                    st状態[i].n相対Y座標_棒 = 4;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if (nNowFrame == 21)
                                {
                                    st状態[i].fX方向拡大率 = 1.70f;
                                    st状態[i].fY方向拡大率 = 0.41f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = 57;
                                    st状態[i].n相対Y座標 = -9;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.6f;
                                    st状態[i].fY方向拡大率_棒 = 0.45f;
                                    st状態[i].n相対X座標_棒 = -98;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if (nNowFrame == 22)
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = 75;
                                    st状態[i].n相対Y座標 = -12;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0.4f;
                                    st状態[i].fY方向拡大率_棒 = 0.25f;
                                    st状態[i].n相対X座標_棒 = -120;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                else if (nNowFrame == 23)
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian(15f);
                                    st状態[i].n相対X座標 = 75;
                                    st状態[i].n相対Y座標 = -12;
                                    st状態[i].n透明度 = 0;

                                    st状態[i].fX方向拡大率_棒 = 0f;
                                    st状態[i].fY方向拡大率_棒 = 0f;
                                    st状態[i].n相対X座標_棒 = -120;
                                    st状態[i].n相対Y座標_棒 = 2;
                                    st状態[i].fZ軸回転度_棒 = CConversion.DegreeToRadian(43f);
                                }
                                #endregion
                                #endregion
                            }
                            else if( st状態[ i ].judge == EJudgement.Good )
                            {
                                #region[ GOOD ]
                                if( nNowFrame == 0 )
                                {
                                    st状態[i].fX方向拡大率 = 0.625f;
                                    st状態[i].fY方向拡大率 = 3.70f;
                                    st状態[i].n相対X座標 = -19;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 1 )
                                {
                                    st状態[i].fX方向拡大率 = 1.125f;
                                    st状態[i].fY方向拡大率 = 2.00f;
                                    st状態[i].n相対X座標 = 4;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 2 )
                                {
                                    st状態[i].fX方向拡大率 = 1.375f;
                                    st状態[i].fY方向拡大率 = 0.66f;
                                    st状態[i].n相対X座標 = 13;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 3 )
                                {
                                    st状態[i].fX方向拡大率 = 1.25f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 8;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame >= 4 && nNowFrame <= 18 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 19 )
                                {
                                    st状態[i].fX方向拡大率 = 1.25f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 8;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 20 )
                                {
                                    st状態[i].fX方向拡大率 = 1.375f;
                                    st状態[i].fY方向拡大率 = 0.66f;
                                    st状態[i].n相対X座標 = 13;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 21 )
                                {
                                    st状態[i].fX方向拡大率 = 1.50f;
                                    st状態[i].fY方向拡大率 = 0.50f;
                                    st状態[i].n相対X座標 = 20;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 22 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].n相対X座標 = 37;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 23 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].n相対X座標 = 37;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                #endregion
                            }
                            else if( st状態[ i ].judge == EJudgement.Poor || st状態[ i ].judge == EJudgement.Miss )
                            {
                                #region[ POOR & MISS ]
                                if( nNowFrame == 0 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = -18;
                                    st状態[i].n透明度 = 100;
                                }
                                else if( nNowFrame == 1 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = -12;
                                    st状態[i].n透明度 = 140;
                                }
                                else if( nNowFrame == 2 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = -6;
                                    st状態[i].n透明度 = 190;
                                }
                                else if( nNowFrame == 3 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 220;
                                }
                                else if( nNowFrame == 4 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = -4;
                                    st状態[i].n透明度 = 255;
                                }
                                else if( nNowFrame == 5 )
                                {
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = -6;
                                    st状態[i].n透明度 = 255;
                                }
                                else if( nNowFrame >= 6 && nNowFrame <= 18 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 255;
                                }
                                else if( nNowFrame == 19 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( -4f );
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 220;
                                }
                                else if( nNowFrame == 20 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( -8f );
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 6;
                                    st状態[i].n透明度 = 190;
                                }
                                else if( nNowFrame == 21 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( -8f );
                                    st状態[i].n相対X座標 = 20;
                                    st状態[i].n相対Y座標 = 12;
                                    st状態[i].n透明度 = 140;
                                }
                                else if( nNowFrame == 22 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( -12f );
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 18;
                                    st状態[i].n透明度 = 100;
                                }
                                else if( nNowFrame == 23 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].fZ軸回転度 = CConversion.DegreeToRadian( -16f );
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 24;
                                    st状態[i].n透明度 = 70;
                                }
                                #endregion
                            }
                            else if( st状態[ i ].judge == EJudgement.Auto )
                            {
                                #region[ Auto ]
                                if( nNowFrame == 0 )
                                {
                                    st状態[i].fX方向拡大率 = 0.625f;
                                    st状態[i].fY方向拡大率 = 3.70f;
                                    st状態[i].n相対X座標 = -19;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 1 )
                                {
                                    st状態[i].fX方向拡大率 = 1.125f;
                                    st状態[i].fY方向拡大率 = 2.00f;
                                    st状態[i].n相対X座標 = 4;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 2 )
                                {
                                    st状態[i].fX方向拡大率 = 1.375f;
                                    st状態[i].fY方向拡大率 = 0.66f;
                                    st状態[i].n相対X座標 = 13;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 3 )
                                {
                                    st状態[i].fX方向拡大率 = 1.25f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 8;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame >= 4 && nNowFrame <= 18 )
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 19 )
                                {
                                    st状態[i].fX方向拡大率 = 1.25f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 8;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 20 )
                                {
                                    st状態[i].fX方向拡大率 = 1.375f;
                                    st状態[i].fY方向拡大率 = 0.66f;
                                    st状態[i].n相対X座標 = 13;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 21 )
                                {
                                    st状態[i].fX方向拡大率 = 1.50f;
                                    st状態[i].fY方向拡大率 = 0.50f;
                                    st状態[i].n相対X座標 = 20;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 22 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].n相対X座標 = 37;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                else if( nNowFrame == 23 )
                                {
                                    st状態[i].fX方向拡大率 = 1.91f;
                                    st状態[i].fY方向拡大率 = 0.23f;
                                    st状態[i].n相対X座標 = 37;
                                    st状態[i].n相対Y座標 = 1;
                                    st状態[i].n透明度 = 0;
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region[ むかしの ]
                    for (int i = 0; i < 12; i++)
                    {
                        if (!st状態[i].ct進行.bStopped)
                        {
                            st状態[i].ct進行.tUpdate();
                            if (st状態[i].ct進行.bReachedEndValue)
                            {
                                st状態[i].ct進行.tStop();
                            }
                            int num2 = st状態[i].ct進行.nCurrentValue;
                            if ((st状態[i].judge != EJudgement.Miss) && (st状態[i].judge != EJudgement.Bad))
                            {
                                if (num2 < 50)
                                {
                                    st状態[i].fX方向拡大率 = 1f + (1f * (1f - (((float)num2) / 50f)));
                                    st状態[i].fY方向拡大率 = ((float)num2) / 50f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0xff;
                                }
                                else if (num2 < 130)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = ((num2 % 6) == 0) ? (CDTXMania.Random.Next(6) - 3) : st状態[i].n相対Y座標;
                                    st状態[i].n透明度 = 0xff;
                                }
                                else if (num2 >= 240)
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f - ((1f * (num2 - 240)) / 60f);
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0xff;
                                }
                                else
                                {
                                    st状態[i].fX方向拡大率 = 1f;
                                    st状態[i].fY方向拡大率 = 1f;
                                    st状態[i].n相対X座標 = 0;
                                    st状態[i].n相対Y座標 = 0;
                                    st状態[i].n透明度 = 0xff;
                                }
                            }
                            else if (num2 < 50)
                            {
                                st状態[i].fX方向拡大率 = 1f;
                                st状態[i].fY方向拡大率 = ((float)num2) / 50f;
                                st状態[i].n相対X座標 = 0;
                                st状態[i].n相対Y座標 = 0;
                                st状態[i].n透明度 = 0xff;
                            }
                            else if (num2 >= 200)
                            {
                                st状態[i].fX方向拡大率 = 1f - (((float)(num2 - 200)) / 100f);
                                st状態[i].fY方向拡大率 = 1f - (((float)(num2 - 200)) / 100f);
                                st状態[i].n相対X座標 = 0;
                                st状態[i].n相対Y座標 = 0;
                                st状態[i].n透明度 = 0xff;
                            }
                            else
                            {
                                st状態[i].fX方向拡大率 = 1f;
                                st状態[i].fY方向拡大率 = 1f;
                                st状態[i].n相対X座標 = 0;
                                st状態[i].n相対Y座標 = 0;
                                st状態[i].n透明度 = 0xff;
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                for (int j = 0; j < 12; j++)
                {
                    if (!st状態[j].ct進行.bStopped)
                    {
                        #region[ 以前まで ]
                        if (CDTXMania.ConfigIni.nJudgeAnimeType < 2)
                        {
                            int num4 = CDTXMania.ConfigIni.nJudgeFrames > 1 ? 0 : st判定文字列[(int)st状態[j].judge].n画像番号;
                            int num5 = 0;
                            int num6 = 0;
                            if (j < 10)
                            {
                                num5 = stレーンサイズ[j].x;
                                if( CDTXMania.ConfigIni.JudgementStringPosition.Drums == EType.A )
                                {
                                    num6 = CDTXMania.ConfigIni.bReverse.Drums ? 348 + -(n文字の縦表示位置[j] * 0x20) : (348 + n文字の縦表示位置[j] * 0x20);
                                }
                                else
                                {
                                    num6 = ( CDTXMania.ConfigIni.bReverse.Drums ? 80 + n文字の縦表示位置[j] * 0x20 : 583 + n文字の縦表示位置[j] * 0x20 );
                                }
                            }

                            int nRectX = CDTXMania.ConfigIni.nJudgeWidgh;
                            int nRectY = CDTXMania.ConfigIni.nJudgeHeight;

                            int xc = (num5 + st状態[j].n相対X座標) + (stレーンサイズ[j].w / 2);
                            int x = (xc - ((int)((110f * st状態[j].fX方向拡大率)))) - ((nRectX - 225) / 2);
                            int y = ((num6 + st状態[j].n相対Y座標) - ((int)(((140f * st状態[j].fY方向拡大率)) / 2.0))) - ((nRectY - 135) / 2);

                            //if (base.tx判定文字列[num4] != null)
                            {
                                if (CDTXMania.ConfigIni.nJudgeFrames > 1 && CDTXMania.stagePerfDrumsScreen.tx判定画像anime != null)
                                {
                                    if (st状態[j].judge == EJudgement.Perfect)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                    if (st状態[j].judge == EJudgement.Great)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 1, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 1, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                    if (st状態[j].judge == EJudgement.Good)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 2, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 2, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                    if (st状態[j].judge == EJudgement.Poor)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 3, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 3, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                    if (st状態[j].judge == EJudgement.Miss)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 4, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 4, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                    if (st状態[j].judge == EJudgement.Auto)
                                    {
                                        //base.tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 5, nRectY * base.st状態[j].nRect, nRectX, nRectY));
                                        CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX * 5, nRectY * st状態[j].nRect, nRectX, nRectY));
                                    }
                                }
                                else if (tx判定文字列[num4] != null)
                                {
                                    x = xc - ((int)((64f * st状態[j].fX方向拡大率)));
                                    y = (num6 + st状態[j].n相対Y座標) - ((int)(((43f * st状態[j].fY方向拡大率)) / 2.0));

                                    tx判定文字列[num4].nTransparency = st状態[j].n透明度;
                                    tx判定文字列[num4].vcScaleRatio = new Vector3(st状態[j].fX方向拡大率, st状態[j].fY方向拡大率, 1f);
                                    tx判定文字列[num4].tDraw2D(CDTXMania.app.Device, x, y, st判定文字列[(int)st状態[j].judge].rc);
                                }


                                if (nShowLagType == (int)EShowLagType.ON ||
                                     ((nShowLagType == (int)EShowLagType.GREAT_POOR) && (st状態[j].judge != EJudgement.Perfect)))
                                {
                                    if (st状態[j].judge != EJudgement.Auto && txlag数値 != null)		// #25370 2011.2.1 yyagi
                                    {
                                        bool minus = false;
                                        int offsetX = 0;
                                        string strDispLag = st状態[j].nLag.ToString();
                                        if (st状態[j].nLag < 0)
                                        {
                                            minus = true;
                                        }
                                        x = xc - strDispLag.Length * 15 / 2;
                                        for (int i = 0; i < strDispLag.Length; i++)
                                        {
                                            int p = (strDispLag[i] == '-') ? 11 : (int)(strDispLag[i] - '0');	//int.Parse(strDispLag[i]);
                                            p += minus ? 0 : 12;		// change color if it is minus value
                                            txlag数値.tDraw2D(CDTXMania.app.Device, x + offsetX, y + 34, stLag数値[p].rc);
                                            offsetX += 15;
                                        }
                                    }
                                }

                            }
                        }
                        #endregion
                        #region[ さいしんばん ]
                        else if( CDTXMania.ConfigIni.nJudgeAnimeType == 2 )
                        {
                            int num4 = 0;
                            int num5 = 0;
                            int num6 = 0;
                            if (j < 10)
                            {
                                num5 = stレーンサイズ[j].x;
                                if( CDTXMania.ConfigIni.JudgementStringPosition.Drums == EType.A )
                                {
                                    num6 = CDTXMania.ConfigIni.bReverse.Drums ? 348 + -(n文字の縦表示位置[j] * 0x20) : (348 + n文字の縦表示位置[j] * 0x20);
                                }
                                else
                                {
                                    num6 = ( CDTXMania.ConfigIni.bReverse.Drums ? 80 + n文字の縦表示位置[j] * 0x20 : 583 + n文字の縦表示位置[j] * 0x20 );
                                }
                            }

                            int nRectX = 85;
                            int nRectY = 35;

                            int xc = ( num5 + st状態[j].n相対X座標 ) + ( stレーンサイズ[j].w / 2 );
                            int yc = ( num6 + st状態[j].n相対Y座標 ) + ( num6 / 2 );
                            float fRot = st状態[j].fZ軸回転度;
                            int x = ( xc - ((int)(((nRectX * st状態[j].fX方向拡大率 ) / st状態[j].fX方向拡大率) * st状態[j].fX方向拡大率)) + (nRectX / 2));
                            int y = ( num6 + st状態[j].n相対Y座標 ) - ((int)((((nRectY) / 2) * st状態[j].fY方向拡大率)));


                            int xc_棒 = ( num5 + st状態[j].n相対X座標_棒) + (stレーンサイズ[j].w / 2);
                            int yc_棒 = ( num6 + st状態[j].n相対Y座標_棒) + (num6 / 2);
                            float fRot_棒 = st状態[j].fZ軸回転度_棒;
                            int x_棒 = ( xc_棒 - ((int)(((nRectX * st状態[j].fX方向拡大率_棒) / st状態[j].fX方向拡大率_棒) * st状態[j].fX方向拡大率_棒)) + (nRectX / 2));
                            int y_棒 = ( num6 + st状態[j].n相対Y座標_棒 ) - ((int)((((nRectY) / 2) * st状態[j].fY方向拡大率_棒)));

                            if (CDTXMania.stagePerfDrumsScreen.tx判定画像anime != null)
                            {
                                if (st状態[j].judge == EJudgement.Perfect)
                                {
                                    
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率_棒, st状態[j].fY方向拡大率_棒, 1f  );
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.fZAxisRotation = st状態[j].fZ軸回転度_棒;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.tDraw2D(CDTXMania.app.Device, x_棒, y_棒, new Rectangle(0, 110, 210, 15));

                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率, st状態[j].fY方向拡大率, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = 255;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, 0, nRectX, nRectY));

                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_3.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率B, st状態[j].fY方向拡大率B, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_3.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_3.nTransparency = st状態[j].n透明度B;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_3.bAdditiveBlending = true;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_3.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, 0, nRectX, nRectY));
                                }
                                if (st状態[j].judge == EJudgement.Great)
                                {
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率_棒, st状態[j].fY方向拡大率_棒, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.fZAxisRotation = st状態[j].fZ軸回転度_棒;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime_2.tDraw2D(CDTXMania.app.Device, x_棒, y_棒, new Rectangle(0, 125, 210, 15));

                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率, st状態[j].fY方向拡大率, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = 255;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX + 5, 0, nRectX, nRectY));
                                }
                                if (st状態[j].judge == EJudgement.Good)
                                {
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率, st状態[j].fY方向拡大率, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = 0;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = 255;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, nRectY + 2, nRectX, nRectY));
                                }
                                if (st状態[j].judge == EJudgement.Poor)
                                {
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(1f, 1f, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = st状態[j].n透明度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(nRectX + 5, nRectY + 2, nRectX, nRectY));
                                }
                                if (st状態[j].judge == EJudgement.Miss)
                                {
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(1f, 1f, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = st状態[j].n透明度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x, y, new Rectangle(0, nRectY * 2 + 4, nRectX, nRectY));
                                }
                                if (st状態[j].judge == EJudgement.Auto)
                                {
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.vcScaleRatio = new Vector3(st状態[j].fX方向拡大率, st状態[j].fY方向拡大率, 1f);
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.fZAxisRotation = st状態[j].fZ軸回転度;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.nTransparency = 255;
                                    CDTXMania.stagePerfDrumsScreen.tx判定画像anime.tDraw2D(CDTXMania.app.Device, x + 5, y, new Rectangle(nRectX * 2 + 3, nRectY * 2 + 4, nRectX, nRectY));
                                }


                                if (nShowLagType == (int)EShowLagType.ON ||
                                     ((nShowLagType == (int)EShowLagType.GREAT_POOR) && (st状態[j].judge != EJudgement.Perfect)))
                                {
                                    if (st状態[j].judge != EJudgement.Auto && txlag数値 != null)		// #25370 2011.2.1 yyagi
                                    {
                                        bool minus = false;
                                        int offsetX = 0;
                                        string strDispLag = st状態[j].nLag.ToString();
                                        if (st状態[j].nLag < 0)
                                        {
                                            minus = true;
                                        }
                                        //x = xc - strDispLag.Length * 15 / 2;
                                        x = ( ( num5 ) + (stレーンサイズ[j].w / 2) ) - strDispLag.Length * 15 / 2;
                                        for (int i = 0; i < strDispLag.Length; i++)
                                        {
                                            int p = (strDispLag[i] == '-') ? 11 : (int)(strDispLag[i] - '0');	//int.Parse(strDispLag[i]);
                                            p += minus ? 0 : 12;		// change color if it is minus value
                                            txlag数値.tDraw2D(CDTXMania.app.Device, x + offsetX, y + 34, stLag数値[p].rc);
                                            offsetX += 15;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
            }
            return 0;
        }

 

 

		

		// Other

		#region [ private ]
		//-----------------
        private readonly int[] n文字の縦表示位置 = new int[] { -1, 1, 1, 2, 0, 0, 1, -1, 2, 1, 2, -1, -1, 0, 0 };
        
		//-----------------
		#endregion
	}
}
