using System.Drawing;
using FDK;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;

namespace DTXMania
{
    internal partial class CActConfigList : CActivity
    {
        // プロパティ

        public bool bIsKeyAssignSelected		// #24525 2011.3.15 yyagi
        {
            get
            {
                EMenuType e = eMenuType;
                if (e == EMenuType.KeyAssignBass || e == EMenuType.KeyAssignDrums ||
                    e == EMenuType.KeyAssignGuitar || e == EMenuType.KeyAssignSystem)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool bIsFocusingParameter => bFocusIsOnElementValue; // #32059 2013.9.17 yyagi

        //Keep these temporarily
        private CItemBase iSystemReturnToMenu;
        private CItemBase iDrumsReturnToMenu;
        private CItemBase iGuitarReturnToMenu;
        private CItemBase iBassReturnToMenu;
        
        public bool b現在選択されている項目はReturnToMenuである
        {
            get
            {
                CItemBase currentItem = listItems[nCurrentSelection];
                if (currentItem == iSystemReturnToMenu || currentItem == iDrumsReturnToMenu ||
                    currentItem == iGuitarReturnToMenu || currentItem == iBassReturnToMenu)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public CItemBase ibCurrentSelection => listItems[nCurrentSelection];
        public int nCurrentSelection;

        /// <summary>
        /// ESC押下時の右メニュー描画
        /// </summary>
        public void tPressEsc()
        {
            switch (eMenuType)
            {
                case EMenuType.KeyAssignSystem:
                    tSetupItemList_System();
                    break;
                case EMenuType.KeyAssignDrums:
                    tSetupItemList_Drums();
                    break;
                case EMenuType.KeyAssignGuitar:
                    tSetupItemList_Guitar();
                    break;
                case EMenuType.KeyAssignBass:
                    tSetupItemList_Bass();
                    break;
            }
        }
        
        public void tPressEnter()
        {
            CDTXMania.Skin.soundDecide.tPlay();
            
            if (bFocusIsOnElementValue)
            {
                bFocusIsOnElementValue = false;
            }
            else if (listItems[nCurrentSelection].eType == CItemBase.EType.Integer)
            {
                bFocusIsOnElementValue = true;
            }
            else
            {
                // Enter押下後の後処理
                listItems[nCurrentSelection].RunAction();
            }
        }   

        private void tGenerateSkinSample()
        {
            nSkinIndex = ((CItemList)listItems[nCurrentSelection]).n現在選択されている項目番号;
            if (nSkinSampleIndex != nSkinIndex)
            {
                string path = skinSubFolders[nSkinIndex];
                path = Path.Combine(path, @"Graphics\2_background.jpg");
                Bitmap bmSrc = new(path);
                Bitmap bmDest = new(1280, 720);
                Graphics g = Graphics.FromImage(bmDest);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmSrc, new Rectangle(60, 106, (int)(1280 * 0.1984), (int)(720 * 0.1984)),
                    0, 0, 1280, 720, GraphicsUnit.Pixel);
                if (txSkinSample1 != null)
                {
                    CDTXMania.tDisposeSafely(ref txSkinSample1);
                }
                txSkinSample1 = CDTXMania.tGenerateTexture(bmDest, false);
                g.Dispose();
                bmDest.Dispose();
                bmSrc.Dispose();
                nSkinSampleIndex = nSkinIndex;
            }
        }

        public void tSetupItemList_Exit()
        {
            tRecordToConfigIni();
            eMenuType = EMenuType.Unknown;
        }
        
        public void tMoveToPrevious()
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            if (bFocusIsOnElementValue)
            {
                listItems[nCurrentSelection].tMoveItemValueToPrevious();
                tPostProcessMoveUpDown();
            }
            else
            {
                nTargetScrollCounter += 100;
                CDTXMania.stageConfig.ctDisplayWait.nCurrentValue = 0;
            }
        }
        public void tMoveToNext()
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            if (bFocusIsOnElementValue)
            {
                listItems[nCurrentSelection].tMoveItemValueToNext();
                tPostProcessMoveUpDown();
            }
            else
            {
                nTargetScrollCounter -= 100;
                CDTXMania.stageConfig.ctDisplayWait.nCurrentValue = 0;
            }
        }
        private void tPostProcessMoveUpDown()  // t要素値を上下に変更中の処理
        {
            if (listItems[nCurrentSelection] == iSystemMasterVolume)              // #33700 2014.4.26 yyagi
            {
                CDTXMania.SoundManager.nMasterVolume = iSystemMasterVolume.nCurrentValue;
            }
        }


        // CActivity 実装

        public override void OnActivate()
        {
            if (bActivated)
                return;

            listItems = new List<CItemBase>();
            eMenuType = EMenuType.Unknown;

            ScanSkinFolders();

            prvFont = new CPrivateFastFont( new FontFamily( CDTXMania.ConfigIni.str選曲リストフォント ), 15 );	// t項目リストの設定 の前に必要

            tSetupItemList_Bass();		// #27795 2012.3.11 yyagi; System設定の中でDrumsの設定を参照しているため、
            tSetupItemList_Guitar();	// 活性化の時点でDrumsの設定も入れ込んでおかないと、System設定中に例外発生することがある。
            tSetupItemList_Drums();	// 
            tSetupItemList_System();	// 順番として、最後にSystemを持ってくること。設定一覧の初期位置がSystemのため。
            
            bFocusIsOnElementValue = false;
            nTargetScrollCounter = 0;
            currentScrollCounter = 0;
            scrollTimerValue = -1;
            ctTriangleArrowAnimation = new CCounter();
            ctToastMessageCounter = new CCounter(0, 1, 10000, CDTXMania.Timer);

            CacheCurrentSoundDevices();
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            if (bNotActivated)
                return;

            tRecordToConfigIni();
            listItems.Clear();
            ctTriangleArrowAnimation = null;

            OnListMenuの解放();
            prvFont.Dispose();

            base.OnDeactivate();

            #region [ Skin変更 ]
            if (CDTXMania.Skin.GetCurrentSkinSubfolderFullName(true) != skinSubFolder_org)
            {
                CDTXMania.stageChangeSkin.tChangeSkinMain();	// #28195 2012.6.11 yyagi CONFIG脱出時にSkin更新
            }
            #endregion

            HandleSoundDeviceChanges();
            
            #region [ サウンドのタイムストレッチモード変更 ]
            CSoundManager.bIsTimeStretch = iSystemTimeStretch.bON;
            #endregion
        }

        public override void OnManagedCreateResources()
        {
            if (bNotActivated)
                return;

            txItemBoxNormal = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox.png"), false);
            txItemBoxOther = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox other.png"), false);
            txTriangleArrow = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_triangle arrow.png"), false);
            txDescriptionPanel = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\4_Description Panel.png" ) );
            txArrow = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\4_Arrow.png" ) );
            txItemBoxCursor = CDTXMania.tGenerateTexture( CSkin.Path( @"Graphics\4_itembox cursor.png" ) );
            txSkinSample1 = null;		// スキン選択時に動的に設定するため、ここでは初期化しない
            prvFontForToastMessage = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.str選曲リストフォント), 14, FontStyle.Regular);
            base.OnManagedCreateResources();
        }
        public override void OnManagedReleaseResources()
        {
            if (bNotActivated)
                return;

            CDTXMania.tReleaseTexture(ref txSkinSample1);
            CDTXMania.tReleaseTexture(ref txItemBoxNormal);
            CDTXMania.tReleaseTexture(ref txItemBoxOther);
            CDTXMania.tReleaseTexture(ref txTriangleArrow);
            CDTXMania.tReleaseTexture( ref txDescriptionPanel );
            CDTXMania.tReleaseTexture( ref txArrow );
            CDTXMania.tReleaseTexture( ref txItemBoxCursor );
            CDTXMania.tReleaseTexture(ref txToastMessage);
            CDTXMania.tDisposeSafely(ref prvFontForToastMessage);
            base.OnManagedReleaseResources();
        }

		private void OnListMenuの初期化()
		{
			OnListMenuの解放();
			listMenu = new stMenuItemRight[ listItems.Count ];
		}

		/// <summary>
		/// 事前にレンダリングしておいたテクスチャを解放する。
		/// </summary>
		private void OnListMenuの解放()
		{
			if ( listMenu != null )
			{
				for ( int i = 0; i < listMenu.Length; i++ )
				{
					if ( listMenu[ i ].txParam != null )
					{
						listMenu[ i ].txParam.Dispose();
					}
					if ( listMenu[ i ].txMenuItemRight != null )
					{
						listMenu[ i ].txMenuItemRight.Dispose();
					}
				}
				listMenu = null;
			}
		}
        public override int OnUpdateAndDraw()
        {
            throw new InvalidOperationException("tUpdateAndDraw(bool)のほうを使用してください。");
        }
        public int tUpdateAndDraw(bool isFocusOnItemList)  // t進行描画 bool b項目リスト側にフォーカスがある
        {
            if (bNotActivated)
                return 0;

            // 進行

            #region [ First update and draw ] //初めての進行描画
            //-----------------
            if (bJustStartedUpdate)
            {
                scrollTimerValue = CSoundManager.rcPerformanceTimer.nCurrentTime;
                ctTriangleArrowAnimation.tStart(0, 9, 50, CDTXMania.Timer);

                bJustStartedUpdate = false;
            }
            //-----------------
            #endregion

            bFocusIsOnItemList = isFocusOnItemList;		// 記憶

            #region [ 項目スクロールの進行 ]
            //-----------------
            long currentTime = CDTXMania.Timer.nCurrentTime;
            if (currentTime < scrollTimerValue) scrollTimerValue = currentTime;

            const int INTERVAL = 2;	// [ms]
            while ((currentTime - scrollTimerValue) >= INTERVAL)
            {
                int scrollDistanceToTarget = Math.Abs((int)(nTargetScrollCounter - currentScrollCounter));
                int scrollAcceleration = 0;

                #region [ n加速度の決定；目標まで遠いほど加速する。]
                //-----------------
                if (scrollDistanceToTarget <= 100)
                {
                    scrollAcceleration = 2;
                }
                else if (scrollDistanceToTarget <= 300)
                {
                    scrollAcceleration = 3;
                }
                else if (scrollDistanceToTarget <= 500)
                {
                    scrollAcceleration = 4;
                }
                else
                {
                    scrollAcceleration = 8;
                }
                //-----------------
                #endregion
                #region [ this.n現在のスクロールカウンタに n加速度 を加減算。]
                //-----------------
                if (currentScrollCounter < nTargetScrollCounter)
                {
                    currentScrollCounter += scrollAcceleration;
                    if (currentScrollCounter > nTargetScrollCounter)
                    {
                        // 目標を超えたら目標値で停止。
                        currentScrollCounter = nTargetScrollCounter;
                    }
                }
                else if (currentScrollCounter > nTargetScrollCounter)
                {
                    currentScrollCounter -= scrollAcceleration;
                    if (currentScrollCounter < nTargetScrollCounter)
                    {
                        // 目標を超えたら目標値で停止。
                        currentScrollCounter = nTargetScrollCounter;
                    }
                }
                //-----------------
                #endregion
                #region [ 行超え処理、ならびに目標位置に到達したらスクロールを停止して項目変更通知を発行。]
                //-----------------
                if (currentScrollCounter >= 100)
                {
                    nCurrentSelection = tNextItem(nCurrentSelection);
                    currentScrollCounter -= 100;
                    nTargetScrollCounter -= 100;
                    if (nTargetScrollCounter == 0)
                    {
                        CDTXMania.stageConfig.tNotifyItemChange();
                    }
                }
                else if (currentScrollCounter <= -100)
                {
                    nCurrentSelection = tPreviousItem(nCurrentSelection);
                    currentScrollCounter += 100;
                    nTargetScrollCounter += 100;
                    if (nTargetScrollCounter == 0)
                    {
                        CDTXMania.stageConfig.tNotifyItemChange();
                    }
                }
                //-----------------
                #endregion

                scrollTimerValue += INTERVAL;
            }
            //-----------------
            #endregion

            #region [ Triangle arrow animation ]
            //-----------------
            if (bFocusIsOnItemList && (nTargetScrollCounter == 0))
                ctTriangleArrowAnimation.tUpdateLoop();
            //-----------------
            #endregion

            #region [ Update Toast Message Counter] 
            ctToastMessageCounter.tUpdate();
            if (ctToastMessageCounter.bReachedEndValue)
            {
                tUpdateToastMessage("");
            }
            #endregion

            // 描画

            basePanelCoordinates[4].X = bFocusIsOnItemList ? 0x228 : 0x25a;		// メニューにフォーカスがあるなら、項目リストの中央は頭を出さない。

            //2014.04.25 kairera0467 GITADORAでは項目パネルが11個だが、選択中のカーソルは中央に無いので両方を同じにすると7×2+1=15個パネルが必要になる。
            //　　　　　　　　　　　 さらに画面に映らないがアニメーション中に見える箇所を含めると17個は必要とされる。
            //　　　　　　　　　　　 ただ、画面に表示させる分には上のほうを考慮しなくてもよさそうなので、上4個は必要なさげ。
            #region [ Draw item panels ]
            //-----------------
            int nItem = nCurrentSelection;
            for (int i = 0; i < 4; i++)
                nItem = tPreviousItem(nItem);

            for (int rowIndex = -4; rowIndex < 10; rowIndex++)		// n行番号 == 0 がフォーカスされている項目パネル。
            {
                #region [ Skip Offscreen Item Panels ]
                //-----------------
                if (((rowIndex == -4) && (currentScrollCounter > 0)) ||		// 上に飛び出そうとしている
                    ((rowIndex == +9) && (currentScrollCounter < 0)))		// 下に飛び出そうとしている
                {
                    nItem = tNextItem(nItem);
                    continue;
                }
                //-----------------
                #endregion
                int n移動元の行の基本位置 = rowIndex + 4;
                int n移動先の行の基本位置 = (currentScrollCounter <= 0) ? ((n移動元の行の基本位置 + 1) % 14) : (((n移動元の行の基本位置 - 1) + 14) % 14);
                int x = pt新パネルの基本座標[n移動元の行の基本位置].X + ((int)((pt新パネルの基本座標[n移動先の行の基本位置].X - pt新パネルの基本座標[n移動元の行の基本位置].X) * (((double)Math.Abs(currentScrollCounter)) / 100.0)));
                int y = pt新パネルの基本座標[n移動元の行の基本位置].Y + ((int)((pt新パネルの基本座標[n移動先の行の基本位置].Y - pt新パネルの基本座標[n移動元の行の基本位置].Y) * (((double)Math.Abs(currentScrollCounter)) / 100.0)));
                int n新項目パネルX = 420;

                #region [ Render Row Panel Frame ]
                //-----------------
                switch (listItems[nItem].ePanelType)
                {
                    case CItemBase.EPanelType.Normal:
                        if (txItemBoxNormal != null)
                            txItemBoxNormal.tDraw2D(CDTXMania.app.Device, n新項目パネルX, y);
                        break;

                    case CItemBase.EPanelType.Other:
                        if (txItemBoxOther != null)
                            txItemBoxOther.tDraw2D(CDTXMania.app.Device, n新項目パネルX, y);
                        break;
                }
                //-----------------
                #endregion
                #region [ Render Item Name ]
                //-----------------
				if ( listMenu[ nItem ].txMenuItemRight != null )	// 自前のキャッシュに含まれているようなら、再レンダリングせずキャッシュを使用
				{
					listMenu[ nItem ].txMenuItemRight.tDraw2D( CDTXMania.app.Device, ( n新項目パネルX + 20 ), ( y + 24 ) );
				}
				else
				{
					Bitmap bmpItem = prvFont.DrawPrivateFont( listItems[ nItem ].strItemName, Color.White, Color.Transparent );
					listMenu[ nItem ].txMenuItemRight = CDTXMania.tGenerateTexture( bmpItem );
//					ctItem.tDraw2D( CDTXMania.app.Device, ( x + 0x12 ) * Scale.X, ( y + 12 ) * Scale.Y - 20 );
//					CDTXMania.tReleaseTexture( ref ctItem );
					CDTXMania.tDisposeSafely( ref bmpItem );
				}
				//CDTXMania.stageConfig.actFont.tDrawString( x + 0x12, y + 12, this.listItems[ nItem ].strItemName );
                //-----------------
                #endregion
                #region [ Render Item Elements ]
				//-----------------
				string strParam = null;
				bool isHighlighted = false;
				switch( listItems[ nItem ].eType )
				{
					case CItemBase.EType.ONorOFFToggle:
						#region [ *** ]
						//-----------------
						//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, ( (CItemToggle) this.listItems[ nItem ] ).bON ? "ON" : "OFF" );
						strParam = ( (CItemToggle) listItems[ nItem ] ).bON ? "ON" : "OFF";
						break;
						//-----------------
						#endregion

					case CItemBase.EType.ONorOFForUndefined3State:
						#region [ *** ]
						//-----------------
						switch( ( (CItemThreeState) listItems[ nItem ] ).e現在の状態 )
						{
							case CItemThreeState.E状態.ON:
								strParam = "ON";
								break;

							case CItemThreeState.E状態.不定:
								strParam = "- -";
								break;

							default:
								strParam = "OFF";
								break;
						}
						//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, "ON" );
						break;
						//-----------------
						#endregion

					case CItemBase.EType.Integer:		// #24789 2011.4.8 yyagi: add PlaySpeed supports (copied them from OPTION)
						#region [ *** ]
						//-----------------
						if( listItems[ nItem ] == iCommonPlaySpeed )
						{
							double d = ( (double) ( (CItemInteger) listItems[ nItem ] ).nCurrentValue ) / 20.0;
							//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, d.ToString( "0.000" ), ( n行番号 == 0 ) && this.bFocusIsOnElementValue );
							strParam = d.ToString( "0.000" );
						}
						else if( listItems[ nItem ] == iDrumsScrollSpeed || listItems[ nItem ] == iGuitarScrollSpeed || listItems[ nItem ] == iBassScrollSpeed )
						{
							float f = ( ( (CItemInteger) listItems[ nItem ] ).nCurrentValue + 1 ) * 0.5f;
							//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, f.ToString( "x0.0" ), ( n行番号 == 0 ) && this.bFocusIsOnElementValue );
							strParam = f.ToString( "x0.0" );
						}
						else
						{
							//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, ( (CItemInteger) this.listItems[ nItem ] ).nCurrentValue.ToString(), ( n行番号 == 0 ) && this.bFocusIsOnElementValue );
							strParam = ( (CItemInteger) listItems[ nItem ] ).nCurrentValue.ToString();
						}
						isHighlighted = ( rowIndex == 0 ) && bFocusIsOnElementValue;
						break;
						//-----------------
						#endregion

					case CItemBase.EType.List:	// #28195 2012.5.2 yyagi: add Skin supports
						#region [ *** ]
						//-----------------
						{
							CItemList list = (CItemList) listItems[ nItem ];
							//CDTXMania.stageConfig.actFont.tDrawString( x + 210, y + 12, list.list項目値[ list.n現在選択されている項目番号 ] );
							strParam = list.list項目値[ list.n現在選択されている項目番号 ];

							#region [ 必要な場合に、Skinのサンプルを生成_描画する。#28195 2012.5.2 yyagi ]
							if ( listItems[ nCurrentSelection ] == iSystemSkinSubfolder )
							{
								tGenerateSkinSample();		// 最初にSkinの選択肢にきたとき(Enterを押す前)に限り、サンプル生成が発生する。

							}
							#endregion
							break;
						}
						//-----------------
						#endregion
				}
				if ( isHighlighted )
				{
					Bitmap bmpStr = isHighlighted ?
						prvFont.DrawPrivateFont( strParam, Color.White, Color.Black, Color.Yellow, Color.OrangeRed ) :
						prvFont.DrawPrivateFont( strParam, Color.Black, Color.Transparent );
					CTexture txStr = CDTXMania.tGenerateTexture( bmpStr, false );
					txStr.tDraw2D( CDTXMania.app.Device, ( n新項目パネルX + 260 ) , ( y + 20 ) );
					CDTXMania.tReleaseTexture( ref txStr );
					CDTXMania.tDisposeSafely( ref bmpStr );
				}
				else
				{
					int nIndex = listItems[ nItem ].GetIndex();
					if ( listMenu[ nItem ].nParam != nIndex || listMenu[ nItem ].txParam == null )
					{
						stMenuItemRight stm = listMenu[ nItem ];
						stm.nParam = nIndex;
						object o = listItems[ nItem ].obj現在値();
						stm.strParam = ( o == null ) ? "" : o.ToString();

				        Bitmap bmpStr =
				            prvFont.DrawPrivateFont( strParam, Color.Black, Color.Transparent );
				        stm.txParam = CDTXMania.tGenerateTexture( bmpStr, false );
				        CDTXMania.tDisposeSafely( ref bmpStr );

				        listMenu[ nItem ] = stm;
				    }
				    listMenu[ nItem ].txParam.tDraw2D( CDTXMania.app.Device, ( n新項目パネルX + 260 ) , ( y + 24 ) );
				}
				//-----------------
                #endregion

                nItem = tNextItem(nItem);
            }
            //-----------------
            #endregion

            #region[ Cursor ]
            if( bFocusIsOnItemList )
            {
                txItemBoxCursor.tDraw2D( CDTXMania.app.Device, 413, 193 );
            }
            #endregion

            #region[ Explanation Panel ]
            if( bFocusIsOnItemList && nTargetScrollCounter == 0 && CDTXMania.stageConfig.ctDisplayWait.bReachedEndValue )
            {
                // 15SEP20 Increasing x position by 180 pixels (was 601)
                txDescriptionPanel.tDraw2D( CDTXMania.app.Device, 781, 252 );
                if ( txSkinSample1 != null && nTargetScrollCounter == 0 && listItems[ nCurrentSelection ] == iSystemSkinSubfolder )
				{
                    // 15SEP20 Increasing x position by 180 pixels (was 615 - 60)
                    txSkinSample1.tDraw2D( CDTXMania.app.Device, 735, 442 - 106 );
				}
            }
            #endregion

            //項目リストにフォーカスがあって、かつスクロールが停止しているなら、パネルの上下に▲印を描画する。
            #region [ Draw a ▲ symbol at the top and bottom when scrolling finishes and the focus is on the item list ]
            //-----------------
            if( bFocusIsOnItemList )//&& (this.nTargetScrollCounter == 0))
            {
                int x;
                int y_upper;
                int y_lower;

                int n新カーソルX = 394;
                int n新カーソル上Y = 174;
                int n新カーソル下Y = 240;

                // 位置決定。

                if (bFocusIsOnElementValue)
                {
                    x = 552;	// 要素値の上下あたり。
                    y_upper = 0x117 - ctTriangleArrowAnimation.nCurrentValue;
                    y_lower = 0x17d + ctTriangleArrowAnimation.nCurrentValue;
                }
                else
                {
                    x = 552;	// 項目名の上下あたり。
                    y_upper = 0x129 - ctTriangleArrowAnimation.nCurrentValue;
                    y_lower = 0x16b + ctTriangleArrowAnimation.nCurrentValue;
                }

                //新矢印
                if( txArrow != null )
                {
                    txArrow.tDraw2D(CDTXMania.app.Device, n新カーソルX, n新カーソル上Y, new Rectangle(0, 0, 40, 40));
                    txArrow.tDraw2D(CDTXMania.app.Device, n新カーソルX, n新カーソル下Y, new Rectangle(0, 40, 40, 40));
                }
            }
            //-----------------
            #endregion

            #region [ Draw Toast Message ]

            if (txToastMessage != null)
            {
                txToastMessage.tDraw2D(CDTXMania.app.Device, 15, 325);
            }
            #endregion

            return 0;
        }


        // Other

        #region [ private ]
        //-----------------
        private enum EMenuType
        {
            System,
            Drums,
            Guitar,
            Bass,
            KeyAssignSystem,		// #24609 2011.4.12 yyagi: 画面キャプチャキーのアサイン
            KeyAssignDrums,
            KeyAssignGuitar,
            KeyAssignBass,
            Unknown
        }

        private bool bFocusIsOnItemList;
        private bool bFocusIsOnElementValue;
        private CCounter ctTriangleArrowAnimation;
        private EMenuType eMenuType;

        private List<CItemBase> listItems;
        private long scrollTimerValue;
        private int currentScrollCounter;
        public int nTargetScrollCounter;
        private Point[] basePanelCoordinates = new Point[] { new(0x25a, 4), new(0x25a, 0x4f), new(0x25a, 0x9a), new(0x25a, 0xe5), new(0x228, 0x130), new(0x25a, 0x17b), new(0x25a, 0x1c6), new(0x25a, 0x211), new(0x25a, 0x25c), new(0x25a, 0x2a7), new(0x25a, 0x2d0) };
        private Point[] pt新パネルの基本座標 = new Point[] { new(0x25a, -79), new(0x25a, -12), new(0x25a, 55), new(0x25a, 122), new(0x228, 189), new(0x25a, 256), new(0x25a, 323), new(0x25a, 390), new(0x25a, 457), new(0x25a, 524), new(0x25a, 591), new(0x25a, 658), new(0x25a, 725), new(0x25a, 792) };
        private CTexture txItemBoxOther;
        private CTexture txTriangleArrow;
        private CTexture txArrow;
        private CTexture txItemBoxNormal;
        private CTexture txItemBoxCursor;
        private CTexture txDescriptionPanel;
        private CTexture txToastMessage;
        private CPrivateFastFont prvFontForToastMessage;
        private CCounter ctToastMessageCounter;

        private CPrivateFastFont prvFont;
        //private List<string> list項目リスト_str最終描画名;
        private struct stMenuItemRight
        {
            //	public string strMenuItem;
            public CTexture txMenuItemRight;
            public int nParam;
            public string strParam;
            public CTexture txParam;
        }
        private stMenuItemRight[] listMenu;
        
        private CItemList iSystemGRmode;
        private CItemInteger iCommonPlaySpeed;
        
        private int tPreviousItem(int nItem)
        {
            if (--nItem < 0)
            {
                nItem = listItems.Count - 1;
            }
            return nItem;
        }
        private int tNextItem(int nItem)
        {
            if (++nItem >= listItems.Count)
            {
                nItem = 0;
            }
            return nItem;
        }

        private void tUpdateDisplayValuesFromConfigIni()
        {
            foreach (CItemBase? item in listItems)
            {
                item.ReadFromConfig();
            }
        }

        private void tRecordToConfigIni()
        {
            foreach (CItemBase? item in listItems)
            {
                item.WriteToConfig();
            }

            if (eMenuType == EMenuType.System)
            {
                CDTXMania.ConfigIni.bGuitarEnabled = (((iSystemGRmode.n現在選択されている項目番号 + 1) / 2) == 1);
                CDTXMania.ConfigIni.bDrumsEnabled = (((iSystemGRmode.n現在選択されている項目番号 + 1) % 2) == 1);

                CDTXMania.ConfigIni.strSystemSkinSubfolderFullName = skinSubFolders[nSkinIndex];				// #28195 2012.5.2 yyagi
                CDTXMania.Skin.SetCurrentSkinSubfolderFullName(CDTXMania.ConfigIni.strSystemSkinSubfolderFullName, true);
            }
        }

        private void tUpdateToastMessage(string strMessage) {
            CDTXMania.tDisposeSafely(ref txToastMessage);

            if (strMessage != "" && prvFontForToastMessage != null)
            {                
                Bitmap bmpItem = prvFontForToastMessage.DrawPrivateFont(strMessage, Color.White, Color.Black);
                txToastMessage = CDTXMania.tGenerateTexture(bmpItem);                
                CDTXMania.tDisposeSafely(ref bmpItem);
            }
            else 
            {
                txToastMessage = null;
            }

        }
        #endregion
    }
}
