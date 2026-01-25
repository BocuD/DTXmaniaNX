using System.Drawing;
using DTXMania.Core;
using DTXMania.UI.Item;
using FDK;
using SharpDX;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;

namespace DTXMania;

internal partial class CActConfigList : CActivity
{
    // プロパティ
    public bool bIsSubMenuSelected // #24525 2011.3.15 yyagi
    {
        get
        {
            EMenuType e = eMenuType;
            if (e is EMenuType.KeyAssignBass or EMenuType.KeyAssignDrums 
                or EMenuType.KeyAssignGuitar or EMenuType.KeyAssignSystem 
                or EMenuType.SystemGraphics or EMenuType.SystemAudio 
                or EMenuType.SystemGameplay or EMenuType.SystemMenu
                or EMenuType.VelocityDrums)
            {
                return true;
            }

            return false;
        }
    }

    public bool bIsFocusingParameter => bFocusIsOnElementValue; // #32059 2013.9.17 yyagi
    
    public bool bCurrentlySelectedItemIsReturnToMenu
    {
        get
        {
            CItemBase currentItem = listItems[nCurrentSelection];
            return currentItem.ePanelType == CItemBase.EPanelType.Return;
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
            case EMenuType.SystemGraphics:
            case EMenuType.SystemAudio:
            case EMenuType.SystemGameplay:
            case EMenuType.SystemMenu:
                tSetupItemList_System();
                break;

            case EMenuType.KeyAssignDrums:
                tSetupItemList_Drums();
                break;
            
            case EMenuType.VelocityDrums:
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
            stageConfig.ctDisplayWait.nCurrentValue = 0;
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
            stageConfig.ctDisplayWait.nCurrentValue = 0;
        }
    }

    private void tPostProcessMoveUpDown() // t要素値を上下に変更中の処理
    {
        if (listItems[nCurrentSelection] == iSystemMasterVolume) // #33700 2014.4.26 yyagi
        {
            CDTXMania.SoundManager.nMasterVolume = iSystemMasterVolume.nCurrentValue;
        }
    }


    // CActivity 実装

    public override void OnActivate()
    {
        if (bActivated)
            return;

        listItems = [];
        eMenuType = EMenuType.Unknown;

        ScanSkinFolders();
        ScanNewSkinData();

        prvFont = new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.songListFont), 15 * CDTXMania.renderScale); // t項目リストの設定 の前に必要

        tSetupItemList_Bass(); // #27795 2012.3.11 yyagi; System設定の中でDrumsの設定を参照しているため、
        tSetupItemList_Guitar(); // 活性化の時点でDrumsの設定も入れ込んでおかないと、System設定中に例外発生することがある。
        tSetupItemList_Drums(); // 
        tSetupItemList_System(); // 順番として、最後にSystemを持ってくること。設定一覧の初期位置がSystemのため。

        bFocusIsOnElementValue = false;
        nTargetScrollCounter = 0;
        currentScrollCounter = 0;
        scrollTimerValue = -1;
        ctTriangleArrowAnimation = new CCounter();
        ctToastMessageCounter = new CCounter(0, 1, 10000, CDTXMania.Timer);

        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        if (!bActivated)
            return;

        tRecordToConfigIni();
        listItems.Clear();
        ctTriangleArrowAnimation = null;

        ReleaseList();
        prvFont.Dispose();

        base.OnDeactivate();

        #region [ Skin変更 ]

        if (CDTXMania.Skin.GetCurrentSkinSubfolderFullName(true) != skinSubFolder_org)
        {
            CDTXMania.StageManager.stageChangeSkin.tChangeSkinMain(); // #28195 2012.6.11 yyagi CONFIG脱出時にSkin更新
        }

        ApplySkinChanges();

        #endregion

        HandleSoundDeviceChanges();

        #region [ サウンドのタイムストレッチモード変更 ]

        CSoundManager.bIsTimeStretch = iSystemTimeStretch.bON;

        #endregion
    }

    public override void OnManagedCreateResources()
    {
        if (!bActivated)
            return;

        txItemBoxNormal = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox.png"), false);
        txItemBoxFolder = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox folder.png"), false);
        
        if (txItemBoxFolder == null)
        {
            txItemBoxFolder = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox.png"), false);
        }
        
        txItemBoxOther = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox other.png"), false);
        txTriangleArrow = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_triangle arrow.png"), false);
        txDescriptionPanel = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_Description Panel.png"));
        txArrow = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_Arrow.png"));
        txItemBoxCursor = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_itembox cursor.png"));
        
        tGenerateSkinSample();
        
        prvFontForToastMessage =
            new CPrivateFastFont(new FontFamily(CDTXMania.ConfigIni.songListFont), 14, FontStyle.Regular);
        base.OnManagedCreateResources();
    }

    public override void OnManagedReleaseResources()
    {
        if (!bActivated)
            return;

        CDTXMania.tReleaseTexture(ref txSkinSample);
        CDTXMania.tReleaseTexture(ref txItemBoxNormal);
        CDTXMania.tReleaseTexture(ref txItemBoxFolder);
        CDTXMania.tReleaseTexture(ref txItemBoxOther);
        CDTXMania.tReleaseTexture(ref txTriangleArrow);
        CDTXMania.tReleaseTexture(ref txDescriptionPanel);
        CDTXMania.tReleaseTexture(ref txArrow);
        CDTXMania.tReleaseTexture(ref txItemBoxCursor);
        CDTXMania.tReleaseTexture(ref txToastMessage);
        CDTXMania.tDisposeSafely(ref prvFontForToastMessage);
        base.OnManagedReleaseResources();
    }

    private void InitializeList()
    {
        ReleaseList();
        listMenu = new stMenuItemRight[listItems.Count];
    }

    /// <summary>
    /// 事前にレンダリングしておいたテクスチャを解放する。
    /// </summary>
    private void ReleaseList()
    {
        for (int i = 0; i < listMenu.Length; i++)
        {
            listMenu[i].Dispose();
        }

        listMenu = [];
    }

    public int tUpdateAndDraw(bool isFocusOnItemList) // t進行描画 bool b項目リスト側にフォーカスがある
    {
        if (!bActivated)
            return 0;

        Matrix scaleMatrix = Matrix.Scaling(CDTXMania.renderScale);
        
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

        bFocusIsOnItemList = isFocusOnItemList; // 記憶

        #region [ 項目スクロールの進行 ]

        //-----------------
        long currentTime = CDTXMania.Timer.nCurrentTime;
        if (currentTime < scrollTimerValue) scrollTimerValue = currentTime;

        const int INTERVAL = 2; // [ms]
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
                    stageConfig.tNotifyItemChange();
                }
            }
            else if (currentScrollCounter <= -100)
            {
                nCurrentSelection = tPreviousItem(nCurrentSelection);
                currentScrollCounter += 100;
                nTargetScrollCounter += 100;
                if (nTargetScrollCounter == 0)
                {
                    stageConfig.tNotifyItemChange();
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

        basePanelCoordinates[4].X = bFocusIsOnItemList ? 552 : 602; // メニューにフォーカスがあるなら、項目リストの中央は頭を出さない。

        //2014.04.25 kairera0467 GITADORAでは項目パネルが11個だが、選択中のカーソルは中央に無いので両方を同じにすると7×2+1=15個パネルが必要になる。
        //　　　　　　　　　　　 さらに画面に映らないがアニメーション中に見える箇所を含めると17個は必要とされる。
        //　　　　　　　　　　　 ただ、画面に表示させる分には上のほうを考慮しなくてもよさそうなので、上4個は必要なさげ。

        #region [ Draw item panels ]
        int nItem = nCurrentSelection;
        for (int i = 0; i < 4; i++)
            nItem = tPreviousItem(nItem);

        for (int rowIndex = -4; rowIndex < 10; rowIndex++) // n行番号 == 0 がフォーカスされている項目パネル。
        {
            nItem = DrawListElement(rowIndex, nItem, scaleMatrix);
        }
        #endregion

        #region[ Draw Cursor ]
        if (bFocusIsOnItemList)
        {
            Matrix cursorMatrix = Matrix.Translation(413, 193, 0) * scaleMatrix;
            txItemBoxCursor.tDraw2DMatrix(CDTXMania.app.Device, cursorMatrix);
        }
        #endregion

        #region[ Draw Explanation Panel ]
        if (bFocusIsOnItemList && nTargetScrollCounter == 0 && stageConfig.ctDisplayWait.bReachedEndValue)
        {
            // 15SEP20 Increasing x position by 180 pixels (was 601)
            Matrix explanationMatrix = Matrix.Translation(781, 252, 0) * scaleMatrix;
            txDescriptionPanel.tDraw2DMatrix(CDTXMania.app.Device, explanationMatrix);
            if (txSkinSample != null && nTargetScrollCounter == 0 &&
                listItems[nCurrentSelection] == iSystemSkinSubfolder)
            {
                // 15SEP20 Increasing x position by 180 pixels (was 615 - 60)
                Matrix txSkinSampleMatrix = Matrix.Translation(735, 442 - 106, 0) * scaleMatrix;
                txSkinSample.tDraw2DMatrix(CDTXMania.app.Device, txSkinSampleMatrix);
            }
        }
        #endregion
        
        #region [ Draw a ▲ symbol at the top and bottom when scrolling finishes and the focus is on the item list ]
        if (bFocusIsOnItemList) //&& (this.nTargetScrollCounter == 0))
        {
            const int nArrowPosX = 394;
            
            Matrix arrowMatrix = Matrix.Translation(nArrowPosX, 174, 0) * scaleMatrix;
            Matrix arrowMatrix2 = Matrix.Translation(nArrowPosX, 240, 0) * scaleMatrix;
            Vector2 size = new(40, 40);
            txArrow?.tDraw2DMatrix(CDTXMania.app.Device, arrowMatrix, size, new SharpDX.RectangleF(0, 0, 40, 40));
            txArrow?.tDraw2DMatrix(CDTXMania.app.Device, arrowMatrix2, size, new SharpDX.RectangleF(0, 40, 40, 40));
        }
        #endregion

        //draw toasat
        Matrix toastMatrix = Matrix.Translation(15, 325, 0) * scaleMatrix;
        txToastMessage?.tDraw2DMatrix(CDTXMania.app.Device, toastMatrix);

        return 0;
    }

    private int DrawListElement(int rowIndex, int nItem, Matrix parentMatrix)
    {
        #region [ Skip Offscreen Item Panels ]

        if ((rowIndex == -4 && currentScrollCounter > 0) || // 上に飛び出そうとしている
            (rowIndex == +9 && currentScrollCounter < 0)) // 下に飛び出そうとしている
        {
            nItem = tNextItem(nItem);
            return nItem;
        }

        #endregion

        int n移動元の行の基本位置 = rowIndex + 4;
        int n移動先の行の基本位置 = currentScrollCounter <= 0
            ? (n移動元の行の基本位置 + 1) % 14
            : (n移動元の行の基本位置 - 1 + 14) % 14;

        int y = pt新パネルの基本座標[n移動元の行の基本位置].Y +
                (int)((pt新パネルの基本座標[n移動先の行の基本位置].Y -
                       pt新パネルの基本座標[n移動元の行の基本位置].Y) *
                      (Math.Abs(currentScrollCounter) / 100.0));

        int n新項目パネルX = 420;

        Matrix localMatrix = Matrix.Translation(n新項目パネルX, y, 0);
        Matrix finalMatrix = localMatrix * parentMatrix;
        
        #region [ Render Row Panel Frame ]

        switch (listItems[nItem].ePanelType)
        {
            case CItemBase.EPanelType.Normal:
                txItemBoxNormal?.tDraw2DMatrix(CDTXMania.app.Device, finalMatrix);
                break;

            case CItemBase.EPanelType.Folder:
                txItemBoxFolder?.tDraw2DMatrix(CDTXMania.app.Device, finalMatrix);
                break;

            case CItemBase.EPanelType.Other:
            case CItemBase.EPanelType.Return:
                txItemBoxOther?.tDraw2DMatrix(CDTXMania.app.Device, finalMatrix);
                break;
        }

        #endregion

        #region [ Render Item Name ]

        if (listMenu[nItem].txMenuItemRight == null)
        {
            Bitmap bmpItem = prvFont.DrawPrivateFont(listItems[nItem].strItemName, Color.White, Color.Transparent);
            listMenu[nItem].txMenuItemRight = CDTXMania.tGenerateTexture(bmpItem);
            bmpItem.Dispose();
        }

        {
            Matrix textMatrix = Matrix.Translation(n新項目パネルX + 25, y + 24, 0) * parentMatrix;

            if (listMenu[nItem].txMenuItemRight != null)
            {
                Vector2 size = new(listMenu[nItem].txMenuItemRight!.szImageSize.Width,
                    listMenu[nItem].txMenuItemRight!.szImageSize.Height);
                size *= 1 / CDTXMania.renderScale;
                listMenu[nItem].txMenuItemRight?.tDraw2DMatrix(CDTXMania.app.Device, textMatrix, size);
            }
        }

        #endregion

        #region [ Render Item Elements ]

        bool isHighlighted = false;

        string strParam = listItems[nItem].GetStringValue();

        switch (listItems[nItem].eType)
        {
            case CItemBase.EType.Integer:
                if (listItems[nItem] == iCommonPlaySpeed)
                {
                    double d = ((CItemInteger)listItems[nItem]).nCurrentValue / 20.0;
                    strParam = d.ToString("0.000");
                }
                else if (listItems[nItem] == iDrumsScrollSpeed ||
                         listItems[nItem] == iGuitarScrollSpeed ||
                         listItems[nItem] == iBassScrollSpeed)
                {
                    float f = (((CItemInteger)listItems[nItem]).nCurrentValue + 1) * 0.5f;
                    strParam = f.ToString("x0.0");
                }

                isHighlighted = rowIndex == 0 && bFocusIsOnElementValue;
                break;
        }

        if (isHighlighted)
        {
            Bitmap bmpStr = prvFont.DrawPrivateFont(strParam, Color.White, Color.Black, Color.Yellow, Color.OrangeRed);
            CTexture txStr = CDTXMania.tGenerateTexture(bmpStr, false);

            Matrix highlightMatrix = Matrix.Translation(n新項目パネルX + 265, y + 20, 0) * parentMatrix;
            Vector2 size = new(txStr.szImageSize.Width, txStr.szImageSize.Height);
            size *= 1 / CDTXMania.renderScale;
            txStr.tDraw2DMatrix(CDTXMania.app.Device, highlightMatrix, size);

            bmpStr.Dispose();
            txStr.Dispose();
        }
        else
        {
            int nIndex = listItems[nItem].GetIndex();
            if (listMenu[nItem].nParam != nIndex || listMenu[nItem].txParam == null)
            {
                stMenuItemRight stm = listMenu[nItem];
                stm.nParam = nIndex;

                Bitmap bmpStr = prvFont.DrawPrivateFont(strParam, Color.Black, Color.Transparent);
                stm.txParam = CDTXMania.tGenerateTexture(bmpStr, false);
                bmpStr.Dispose();

                listMenu[nItem] = stm;
            }

            Matrix paramMatrix = Matrix.Translation(n新項目パネルX + 265, y + 24, 0) * parentMatrix;
            if (listMenu[nItem].txParam != null)
            {
                Vector2 size = new(listMenu[nItem].txParam!.szImageSize.Width, listMenu[nItem].txParam!.szImageSize.Height);
                size *= 1 / CDTXMania.renderScale;
                listMenu[nItem].txParam?.tDraw2DMatrix(CDTXMania.app.Device, paramMatrix, size);
            }
        }

        #endregion

        nItem = tNextItem(nItem);
        return nItem;
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
        KeyAssignSystem, // #24609 2011.4.12 yyagi: 画面キャプチャキーのアサイン
        SystemGraphics,
        SystemAudio,
        SystemGameplay,
        SystemMenu,
        KeyAssignDrums,
        VelocityDrums,
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

    private Point[] basePanelCoordinates =
    [
        new(602, 4), new(602, 79), new(602, 154), new(602, 229), new(552, 304), new(602, 379), new(602, 454),
        new(602, 529), new(602, 604), new(602, 679), new(602, 720)
    ];

    private Point[] pt新パネルの基本座標 =
    [
        new(602, -79), new(602, -12), new(602, 55), new(602, 122), new(552, 189), new(602, 256), new(602, 323),
        new(602, 390), new(602, 457), new(602, 524), new(602, 591), new(602, 658), new(602, 725), new(602, 792)
    ];

    private CTexture txItemBoxOther;
    private CTexture txTriangleArrow;
    private CTexture txArrow;
    private CTexture txItemBoxNormal;
    private CTexture txItemBoxFolder;
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
        public CTexture? txMenuItemRight;
        public int nParam;
        public CTexture? txParam;
        
        public void Dispose()
        {
            txMenuItemRight?.Dispose();
            txParam?.Dispose();
        }
    }

    private stMenuItemRight[] listMenu = [];

    private CItemList iSystemGRmode;
    private CItemInteger iCommonPlaySpeed;

    private CStageConfig stageConfig;

    public CActConfigList(CStageConfig cStageConfig)
    {
        stageConfig = cStageConfig;
    }

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
            CDTXMania.ConfigIni.bGuitarEnabled = (((iSystemGRmode.nCurrentlySelectedIndex + 1) / 2) == 1);
            CDTXMania.ConfigIni.bDrumsEnabled = (((iSystemGRmode.nCurrentlySelectedIndex + 1) % 2) == 1);

            CDTXMania.ConfigIni.strSystemSkinSubfolderFullName = skinSubFolders[nSkinIndex]; // #28195 2012.5.2 yyagi
            CDTXMania.Skin.SetCurrentSkinSubfolderFullName(CDTXMania.ConfigIni.strSystemSkinSubfolderFullName, true);
        }
    }

    private void tUpdateToastMessage(string strMessage)
    {
        CDTXMania.tReleaseTexture(ref txToastMessage);

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

    private void tAddReturnToMenuItem(Action? action = null)
    {
        var returnToMenuButton = new CItemBase("<< Back", CItemBase.EPanelType.Return,
            "前のメニューに戻ります。",
            "Return to the previous menu.") 
        {
            action = action
        };
        listItems.Add(returnToMenuButton);
    }

    #endregion
}