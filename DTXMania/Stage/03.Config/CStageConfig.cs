using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;
using FDK;
using SharpDX;
using RectangleF = SharpDX.RectangleF;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageConfig : CStage
{
    // プロパティ

    public CActDFPFont actFont { get; private set; }

    // コンストラクタ

    public CStageConfig()
    {
        CActDFPFont font;
        eStageID = EStage.Config_3;
        ePhaseID = EPhase.Common_DefaultState;
        actFont = font = new CActDFPFont();
        listChildActivities.Add(font);
        listChildActivities.Add(actFIFO = new CActFIFOWhite());
        listChildActivities.Add(actList = new CActConfigList(this));
        listChildActivities.Add(actKeyAssign = new CActConfigKeyAssign(this));
        bActivated = false;
    }


    // メソッド

    public void tNotifyAssignmentComplete()
    {
        eItemPanelMode = EItemPanelMode.PadList;
    }
    public void tNotifyPadSelection(EKeyConfigPart part, EKeyConfigPad pad)
    {
        actKeyAssign.tStart(part, pad, actList.ibCurrentSelection.strItemName);
        eItemPanelMode = EItemPanelMode.KeyCodeList;
    }
    public void tNotifyItemChange()
    {
        tDrawSelectedItemDescriptionInDescriptionPanel();
    }
        
    // CStage 実装

    public override void InitializeBaseUI()
    {
        //left menu
        UIGroup leftMenu = ui.AddChild(new UIGroup("Left Options Menu"));
        leftMenu.position = new Vector3(245, 140, 0);
        leftMenu.renderOrder = 50;
        leftMenu.dontSerialize = true;
        
        DTXTexture menuPanelTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_menu panel.png")));
        UIImage menuPanel = leftMenu.AddChild(new UIImage(menuPanelTex));
        menuPanel.position = Vector3.Zero;
            
        //menu items
        configLeftOptionsMenu = leftMenu.AddChild(new UISelectList("Button List"));
        configLeftOptionsMenu.isVisible = true;
        configLeftOptionsMenu.dontSerialize = true;
        
        //340 - size/2, so this becomes 340-245= 95
        configLeftOptionsMenu.position = new Vector3(95, 4, 0);

        //todo: render menu cursor correctly to match current version of the game. right now its rendered as a stretched image.
        DTXTexture menuCursorTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_menu cursor.png")));
        menuCursor = configLeftOptionsMenu.AddChild(new UIImage(menuCursorTex));
        menuCursor.position = new Vector3(-5, 2, 0);
        menuCursor.size = new Vector2(170, 28);
        menuCursor.anchor = new Vector2(0.5f, 0f);
        menuCursor.renderMode = ERenderMode.Sliced;
        menuCursor.sliceRect = new RectangleF(16, 0, 32, 28);

        var family = new FontFamily(CDTXMania.ConfigIni.songListFont);
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 18, "System", () => { actList.tSetupItemList_System(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 18, "Drums", () => { actList.tSetupItemList_Drums(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 18, "Guitar", () => { actList.tSetupItemList_Guitar(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 18, "Bass", () => { actList.tSetupItemList_Bass(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 18, "Exit", () => { actList.tSetupItemList_Exit(); }));
        configLeftOptionsMenu.UpdateLayout();
        configLeftOptionsMenu.SetSelectedIndex(0);
        
        if (bFocusIsOnMenu)
        {
            tDrawSelectedMenuDescriptionInDescriptionPanel();
        }
        else
        {
            tDrawSelectedItemDescriptionInDescriptionPanel();
        }
    }

    public override void InitializeDefaultUI()
    {
        //create resources for menu elements
        DTXTexture bgTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_background.png")));
        UIImage bg = ui.AddChild(new UIImage(bgTex));
        bg.renderOrder = -100;
        bg.position = Vector3.Zero;
                
        DTXTexture itemBarTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_item bar.png")));
        UIImage itemBar = ui.AddChild(new UIImage(itemBarTex));
        itemBar.position = new Vector3(400, 0, 0);
        itemBar.renderOrder = 50;
                
        DTXTexture headerPanelTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_header panel.png")));
        UIImage headerPanel = ui.AddChild(new UIImage(headerPanelTex));
        headerPanel.position = Vector3.Zero;
        headerPanel.renderOrder = 52;
                
        DTXTexture footerPanelTex = new(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\4_footer panel.png")));
        UIImage footerPanel = ui.AddChild(new UIImage(footerPanelTex));
        footerPanel.position = new Vector3(0, 720 - footerPanel.Texture.Height, 0);
        footerPanel.renderOrder = 53;
    }

    public override void OnActivate()
    {
        Trace.TraceInformation("コンフィグステージを活性化します。");
        Trace.Indent();
        try
        {
            configLeftOptionsMenu?.SetSelectedIndex(0);
            ftFont = new Font("MS PGothic", 17f, FontStyle.Regular, GraphicsUnit.Pixel);
            for (int i = 0; i < 4; i++)
            {
                ctKeyRepetition[i] = new CCounter(0, 0, 0, CDTXMania.Timer);
            }
            bFocusIsOnMenu = true;
            eItemPanelMode = EItemPanelMode.PadList;
            ctDisplayWait = new CCounter( 0, 350, 1, CDTXMania.Timer );
        }
        finally
        {
            Trace.TraceInformation("コンフィグステージの活性化を完了しました。");
            Trace.Unindent();
        }
        base.OnActivate();		// 2011.3.14 yyagi: OnActivate()をtryの中から外に移動
    }
    public override void OnDeactivate()
    {
        Trace.TraceInformation("コンフィグステージを非活性化します。");
        Trace.Indent();
        try
        {
            CDTXMania.ConfigIni.tWrite(CDTXMania.executableDirectory + "Config.ini");	// CONFIGだけ
            if (ftFont != null)													// 以下OPTIONと共通
            {
                ftFont.Dispose();
                ftFont = null;
            }
            for (int i = 0; i < 4; i++)
            {
                ctKeyRepetition[i] = null;
            }
            ctDisplayWait = null;
            base.OnDeactivate();
        }
        catch (UnauthorizedAccessException e)
        {
            Trace.TraceError(e.Message + "ファイルが読み取り専用になっていないか、管理者権限がないと書き込めなくなっていないか等を確認して下さい");
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message);
        }
        finally
        {
            Trace.TraceInformation("コンフィグステージの非活性化を完了しました。");
            Trace.Unindent();
        }
    }

    private UISelectList configLeftOptionsMenu;
    private UIImage menuCursor;
    
    public override void OnManagedReleaseResources()											// OPTIONと同じ(COnfig.iniの書き出しタイミングのみ異なるが、無視して良い)
    {
        if (bActivated)
        {
            CDTXMania.tReleaseTexture(ref txDescriptionPanel);
            
            base.OnManagedReleaseResources();
        }
    }

    public override void FirstUpdate()
    {
        ePhaseID = EPhase.Common_FadeIn;
        actFIFO.tStartFadeIn();
    }

    public override int OnUpdateAndDraw()
    {
        if (!bActivated) return 0;
        
        base.OnUpdateAndDraw();
        
        ctDisplayWait.tUpdate();
            
        //update menu cursor position
        menuCursor.Texture.transparency = bFocusIsOnMenu ? 1.0f : 0.5f;
        menuCursor.position.Y = 2 + configLeftOptionsMenu.currentlySelectedIndex * 32;
        
        #region [ アイテム ]
        //---------------------
        switch (eItemPanelMode)
        {
            case EItemPanelMode.PadList:
                actList.tUpdateAndDraw(!bFocusIsOnMenu);
                break;

            case EItemPanelMode.KeyCodeList:
                actKeyAssign.OnUpdateAndDraw();
                break;
        }
        //---------------------
        #endregion
        #region [ Description panel ]
        //---------------------
        if( txDescriptionPanel != null && !bFocusIsOnMenu && actList.nTargetScrollCounter == 0 && ctDisplayWait.bReachedEndValue )
            // 15SEP20 Increasing x position by 180 pixels (was 620)
            txDescriptionPanel.tDraw2D(CDTXMania.app.Device, 800, 270);
        //---------------------
        #endregion

        #region [ Fade in and out ]
        //---------------------
        switch (ePhaseID)
        {
            case EPhase.Common_FadeIn:
                if (actFIFO.OnUpdateAndDraw() != 0)
                {
                    CDTXMania.Skin.bgmコンフィグ画面.tPlay();
                    ePhaseID = EPhase.Common_DefaultState;
                }
                break;

            case EPhase.Common_FadeOut:
                if (actFIFO.OnUpdateAndDraw() == 0)
                {
                    break;
                }
                return 1;
        }
        //---------------------
        #endregion
            
        // キー入力

        if ((ePhaseID != EPhase.Common_DefaultState) || actKeyAssign.bWaitingForKeyInput)
            return 0;

        // 曲データの一覧取得中は、キー入力を無効化する
        if (!CDTXMania.EnumSongs.IsEnumerating || CDTXMania.actEnumSongs.bコマンドでの曲データ取得 != true)
        {
            if (CDTXMania.Input.ActionCancel())
            {
                CDTXMania.Skin.soundCancel.tPlay();
                if (!bFocusIsOnMenu)
                {
                    if (eItemPanelMode == EItemPanelMode.KeyCodeList)
                    {
                        tNotifyAssignmentComplete();
                        return 0;
                    }
                    if (!actList.bIsSubMenuSelected && !actList.bIsFocusingParameter)	// #24525 2011.3.15 yyagi, #32059 2013.9.17 yyagi
                    {
                        bFocusIsOnMenu = true;
                    }
                    tDrawSelectedMenuDescriptionInDescriptionPanel();
                    actList.tPressEsc();								// #24525 2011.3.15 yyagi ESC押下時の右メニュー描画用
                }
                else
                {
                    actFIFO.tStartFadeOut();
                    ePhaseID = EPhase.Common_FadeOut;
                }
            }
            else if (CDTXMania.Input.ActionDecide())
            {
                if (configLeftOptionsMenu.currentlySelectedIndex == 4)
                {
                    CDTXMania.Skin.soundDecide.tPlay();
                    actFIFO.tStartFadeOut();
                    ePhaseID = EPhase.Common_FadeOut;
                }
                else if (bFocusIsOnMenu)
                {
                    CDTXMania.Skin.soundDecide.tPlay();
                    bFocusIsOnMenu = false;
                    tDrawSelectedItemDescriptionInDescriptionPanel();
                }
                else
                {
                    switch (eItemPanelMode)
                    {
                        case EItemPanelMode.PadList:
                            bool bIsKeyAssignSelectedBeforeHitEnter = actList.bIsSubMenuSelected;	// #24525 2011.3.15 yyagi
                            actList.tPressEnter();
                            if (actList.bCurrentlySelectedItemIsReturnToMenu)
                            {
                                tDrawSelectedMenuDescriptionInDescriptionPanel();
                                if (bIsKeyAssignSelectedBeforeHitEnter == false)							// #24525 2011.3.15 yyagi
                                {
                                    bFocusIsOnMenu = true;
                                }
                            }
                            break;

                        case EItemPanelMode.KeyCodeList:
                            actKeyAssign.tPressEnter();
                            break;
                    }
                }
            }
            ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow), new CCounter.DGキー処理(tMoveCursorUp));
            ctKeyRepetition.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.HH), new CCounter.DGキー処理(tMoveCursorUp));
            //Change to HT
            if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
            {
                tMoveCursorUp();
            }
            ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow), new CCounter.DGキー処理(tMoveCursorDown));
            ctKeyRepetition.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.SD), new CCounter.DGキー処理(tMoveCursorDown));
            //Change to LT
            if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
            {
                tMoveCursorDown();
            }
        }
        
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    private enum EItemPanelMode
    {
        PadList,
        KeyCodeList
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STKeyRepetitionCounter
    {
        public CCounter Up;
        public CCounter Down;
        public CCounter R;
        public CCounter B;
        public CCounter this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Up;

                    case 1:
                        return Down;

                    case 2:
                        return R;

                    case 3:
                        return B;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Up = value;
                        return;

                    case 1:
                        Down = value;
                        return;

                    case 2:
                        R = value;
                        return;

                    case 3:
                        B = value;
                        return;
                }
                throw new IndexOutOfRangeException();
            }
        }
    }

    private CActFIFOWhite actFIFO;
    private CActConfigKeyAssign actKeyAssign;
    private CActConfigList actList;
    
    private bool bFocusIsOnMenu;
    private STKeyRepetitionCounter ctKeyRepetition;
    private EItemPanelMode eItemPanelMode;
    private Font ftFont;
        
    private CTexture txDescriptionPanel;
    public CCounter ctDisplayWait;
        
    private void tMoveCursorDown()
    {
        if (!bFocusIsOnMenu)
        {
            switch (eItemPanelMode)
            {
                case EItemPanelMode.PadList:
                    actList.tMoveToPrevious();
                    return;

                case EItemPanelMode.KeyCodeList:
                    actKeyAssign.tMoveToNext();
                    return;
            }
        }
        else
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            ctDisplayWait.nCurrentValue = 0;
                
            configLeftOptionsMenu.SelectNext();
            configLeftOptionsMenu.RunAction();
                
            tDrawSelectedMenuDescriptionInDescriptionPanel();
        }
    }
    private void tMoveCursorUp()
    {
        if (!bFocusIsOnMenu)
        {
            switch (eItemPanelMode)
            {
                case EItemPanelMode.PadList:
                    actList.tMoveToNext();
                    return;

                case EItemPanelMode.KeyCodeList:
                    actKeyAssign.tMoveToPrevious();
                    return;
            }
        }
        else
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            ctDisplayWait.nCurrentValue = 0;
                
            configLeftOptionsMenu.SelectPrevious();
            configLeftOptionsMenu.RunAction();
                
            tDrawSelectedMenuDescriptionInDescriptionPanel();
        }
    }
    private void tDrawSelectedMenuDescriptionInDescriptionPanel()
    {
        try
        {
            Bitmap image = new(220 * 2, 192 * 2); // 説明文領域サイズの縦横 2 倍。（描画時に 0.5 倍で表示する。）
            Graphics graphics = Graphics.FromImage(image);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            string[,] str = new string[2, 2];
            switch (configLeftOptionsMenu.currentlySelectedIndex)
            {
                case 0: //system
                    str[0, 0] = "システムに関係する項目を設定します。";
                    str[0, 1] = "";
                    str[1, 0] = "Settings for an overall systems.";
                    break;

                case 1: //drums
                    str[0, 0] = "ドラムの演奏に関する項目を設定します。";
                    str[0, 1] = "";
                    str[1, 0] = "Settings to play the drums.";
                    str[1, 1] = "";
                    break;

                case 2: //guitar
                    str[0, 0] = "ギターの演奏に関する項目を設定します。";
                    str[0, 1] = "";
                    str[1, 0] = "Settings to play the guitar.";
                    str[1, 1] = "";
                    break;

                case 3: //bass
                    str[0, 0] = "ベースの演奏に関する項目を設定します。";
                    str[0, 1] = "";
                    str[1, 0] = "Settings to play the bass.";
                    str[1, 1] = "";
                    break;

                case 4: //exit
                    str[0, 0] = "設定を保存し、コンフィグ画面を終了します。";
                    str[0, 1] = "";
                    str[1, 0] = "Save the settings and exit from\nCONFIGURATION menu.";
                    str[1, 1] = "";
                    break;
            }

            int c = CDTXMania.isJapanese ? 0 : 1;
            for (int i = 0; i < 2; i++)
            {
                graphics.DrawString(str[c, i], ftFont, Brushes.Black, new PointF(4f, (i * 30)));
            }

            graphics.Dispose();
            if (txDescriptionPanel != null)
            {
                txDescriptionPanel.Dispose();
            }

            //this.txDescriptionPanel = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
            // this.txDescriptionPanel.vcScaleRatio.X = 0.5f;
            // this.txDescriptionPanel.vcScaleRatio.Y = 0.5f;
            image.Dispose();
        }
        catch (CTextureCreateFailedException)
        {
            Trace.TraceError("説明文テクスチャの作成に失敗しました。");
            txDescriptionPanel = null;
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message + "\nTrace: " + e.StackTrace);
        }
    }
    private void tDrawSelectedItemDescriptionInDescriptionPanel()
    {
        try
        {
            Bitmap image = new( 400, 192 );		// 説明文領域サイズの縦横 2 倍。（描画時に 0.5 倍で表示する___のは中止。処理速度向上のため。）
            Graphics graphics = Graphics.FromImage( image );
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            CItemBase item = actList.ibCurrentSelection;
            if( item.strDescription is { Length: > 0 } )
            {
                graphics.DrawString( item.strDescription, ftFont, Brushes.Black, new System.Drawing.RectangleF( 4f, (float) 0, 230, 430 ) );
            }
            graphics.Dispose();
            if( txDescriptionPanel != null )
            {
                txDescriptionPanel.Dispose();
            }
            txDescriptionPanel = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat, false );
            image.Dispose();
        }
        catch( CTextureCreateFailedException )
        {
            Trace.TraceError( "説明文パネルテクスチャの作成に失敗しました。" );
            txDescriptionPanel = null;
        }
    }
    //-----------------
    #endregion
}