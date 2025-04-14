using System.Runtime.InteropServices;
using DTXMania.Core;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;
using DTXUIRenderer;
using FDK;
using SharpDX;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CActSelectPopupMenu : CActivity
{

    // プロパティ
    public int GetIndex(int pos)
    {
        return lciMenuItems[pos].GetIndex();
    }
    public object GetObj現在値(int pos)
    {
        return lciMenuItems[pos].GetCurrentValue();
    }
    public bool bGotoDetailConfig
    {
        get;
        internal set;
    }

    /// <summary>
    /// ソートメニュー機能を使用中かどうか。外部からこれをtrueにすると、ソートメニューが出現する。falseにすると消える。
    /// </summary>
    public bool bIsActivePopupMenu
    {
        get;
        private set;
    }
    public virtual void tActivatePopupMenu(EInstrumentPart einst)
    {
        eInst = einst;
        bIsActivePopupMenu = true;
        bIsSelectingIntItem = false;
        bGotoDetailConfig = false;
    }
    public virtual void tDeativatePopupMenu()
    {
        bIsActivePopupMenu = false;
    }


    struct ItemPair
    {
        public UIDFPText name;
        public UIDFPText value;
    }

    private List<ItemPair> listItems = new List<ItemPair>();
        
    public void Initialize(List<CItemBase> menulist, string title, int defaultPos)
    {
        strMenuTitle = title;
        lciMenuItems = menulist;
        nCurrentSelection = defaultPos;

        if (menuItems == null) return;
            
        //remove current lci items
        menuItems.ClearChildren();

        listItems.Clear();
        listItems = new List<ItemPair>();
            
        //add new lci items
        for (int i = 0; i < lciMenuItems.Count; i++)
        {
            var item = menuItems.AddChild(new UIDFPText(font, lciMenuItems[i].strItemName));
            item.position = new Vector3(18, 40 + i * 32, 0);
            item.renderOrder = 1;
                
            var value = menuItems.AddChild(new UIDFPText(font, ""));
            value.position = new Vector3(200, 40 + i * 32, 0);
            value.renderOrder = 1;

            listItems.Add(new ItemPair() {name = item, value = value});
        }
    }


    public void tPressEnter()
    {
        if (bキー入力待ち)
        {
            CDTXMania.Skin.soundDecide.tPlay();

            if (nCurrentSelection != lciMenuItems.Count - 1)
            {
                lciMenuItems[nCurrentSelection].RunAction();
                    
                if (lciMenuItems[nCurrentSelection].eType == CItemBase.EType.Integer)
                {
                    bIsSelectingIntItem = !bIsSelectingIntItem;		// 選択状態/選択解除状態を反転する
                }
            }
            tPressEnterMain();

            bキー入力待ち = true;
        }
    }

    /// <summary>
    /// Decide押下時の処理を、継承先で記述する。
    /// </summary>
    public virtual void tPressEnterMain()
    {
    }
    /// <summary>
    /// Cancel押下時の追加処理があれば、継承先で記述する。
    /// </summary>
    public virtual void tCancel()
    {
    }
    /// <summary>
    /// BD二回入力時の追加処理があれば、継承先で記述する。
    /// </summary>
    public virtual void tBDContinuity()
    {
    }
    /// <summary>
    /// 追加の描画処理。必要に応じて、継承先で記述する。
    /// </summary>
    public virtual void tDrawSub()
    {
    }


    public void tMoveToNext()
    {
        if (bキー入力待ち)
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            if (bIsSelectingIntItem)
            {
                lciMenuItems[nCurrentSelection].tMoveItemValueToPrevious();		// 項目移動と数値上下は方向が逆になるので注意
            }
            else
            {
                if (++nCurrentSelection >= lciMenuItems.Count)
                {
                    nCurrentSelection = 0;
                }
            }
        }
    }
    public void tMoveToPrevious()
    {
        if (bキー入力待ち)
        {
            CDTXMania.Skin.soundCursorMovement.tPlay();
            if (bIsSelectingIntItem)
            {
                lciMenuItems[nCurrentSelection].tMoveItemValueToNext();		// 項目移動と数値上下は方向が逆になるので注意
            }
            else
            {
                if (--nCurrentSelection < 0)
                {
                    nCurrentSelection = lciMenuItems.Count - 1;
                }
            }
        }
    }

    // CActivity 実装

    public override void OnActivate()
    {
        //		this.nSelectedRow = 0;
        bキー入力待ち = true;
        for (int i = 0; i < 4; i++)
        {
            ctキー反復用[i] = new CCounter(0, 0, 0, CDTXMania.Timer);
        }
        bNotActivated = true;

        bIsActivePopupMenu = false;
        font = new CActDFPFont();
        listChildActivities.Add(font);

        CommandHistory = new CStageSongSelection.CCommandHistory();
        base.OnActivate();
    }
    public override void OnDeactivate()
    {
        if (!bNotActivated)
        {
            listChildActivities.Remove(font);
            font.OnDeactivate();
            font = null;

            ui.Dispose();
                
            for (int i = 0; i < 4; i++)
            {
                ctキー反復用[i] = null;
            }
            base.OnDeactivate();
        }
    }

    public override void OnManagedCreateResources()
    {
        if (!bNotActivated)
        {
            ui = CDTXMania.stageSongSelection.ui.AddChild(new UIGroup("Popup Menu"));
            ui.position = new Vector3(1280.0f/2.0f, 720.0f/2.0f + 20.0f, 0); 
            ui.anchor = new Vector2(0.5f, 0.5f);
            ui.renderOrder = 100;
            ui.isVisible = false;
            ui.dontSerialize = true;
                
            var bgTex = new DTXTexture(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenSelect sort menu background.png"), false));
            var bg = ui.AddChild(new UIImage(bgTex));
            ui.size = bg.size;
                
            var cursorTex = new DTXTexture(CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenConfig menu cursor.png"), false));
            cursor = ui.AddChild(new UIImage(cursorTex));
            cursor.position = new Vector3(12, 32 + 6, 0);
            cursor.size = new Vector2(336, 32);
            cursor.renderMode = ERenderMode.Sliced;
            cursor.sliceRect = new RectangleF(8, 0, 16, 32);
                
            menuItems = ui.AddChild(new UIGroup("Menu Items"));
                
            var menuText = ui.AddChild(new UIDFPText(font, strMenuTitle));
            menuText.position = new Vector3(96.0f, 4.0f, 0);
                
            base.OnManagedCreateResources();
        }
    }
    public override void OnManagedReleaseResources()
    {
        if (!bNotActivated)
        {
            ui.Dispose();
        }
        base.OnManagedReleaseResources();
    }

    public override int OnUpdateAndDraw()
    {
        throw new InvalidOperationException("tUpdateAndDraw(bool)のほうを使用してください。");
    }

    public int tUpdateAndDraw()  // t進行描画
    {
        if (!bNotActivated && bIsActivePopupMenu)
        {
            if (bキー入力待ち)
            {
                #region [ CONFIG画面 ]
                if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.Help))
                {	// [SHIFT] + [F1] CONFIG
                    CDTXMania.Skin.soundCancel.tPlay();
                    tCancel();
                    bGotoDetailConfig = true;
                }
                #endregion
                #region [ キー入力: キャンセル ]
                else if (CDTXMania.Input.ActionCancel())
                {	// キャンセル
                    CDTXMania.Skin.soundCancel.tPlay();
                    tCancel();
                    bIsActivePopupMenu = false;
                }
                #endregion
                #region [ BD二回: キャンセル ]
                else if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.BD))
                {	// キャンセル
                    CommandHistory.Add(EInstrumentPart.DRUMS, EPadFlag.BD);
                    EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.BD, EPadFlag.BD };
                    if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.DRUMS))
                    {
                        CDTXMania.Skin.soundChange.tPlay();
                        tBDContinuity();
                        bIsActivePopupMenu = false;
                    }
                }
                #endregion
                #region [ Px2 Guitar: 簡易CONFIG ]
                if (CDTXMania.Pad.bPressed(EInstrumentPart.GUITAR, EPad.P))
                {	// [BD]x2 スクロール速度変更
                    CommandHistory.Add(EInstrumentPart.GUITAR, EPadFlag.P);
                    EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.P, EPadFlag.P };
                    if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.GUITAR))
                    {
                        CDTXMania.Skin.soundChange.tPlay();
                        tBDContinuity();
                        bIsActivePopupMenu = false;
                    }
                }
                #endregion
                #region [ Px2 Bass: 簡易CONFIG ]
                if (CDTXMania.Pad.bPressed(EInstrumentPart.BASS, EPad.P))
                {	// [BD]x2 スクロール速度変更
                    CommandHistory.Add(EInstrumentPart.BASS, EPadFlag.P);
                    EPadFlag[] comChangeScrollSpeed = new EPadFlag[] { EPadFlag.P, EPadFlag.P };
                    if (CommandHistory.CheckCommand(comChangeScrollSpeed, EInstrumentPart.BASS))
                    {
                        CDTXMania.Skin.soundChange.tPlay();
                        tBDContinuity();
                        bIsActivePopupMenu = false;
                    }
                }
                #endregion

                #region [ キー入力: 決定 ]
                // EInstrumentPart eInst = EInstrumentPart.UNKNOWN;
                ESortAction eAction = ESortAction.END;
                if (CDTXMania.Input.ActionDecide())
                {
                    eAction = ESortAction.Decide;
                }
                    
                if (eAction == ESortAction.Decide)	// 決定
                {
                    tPressEnter();
                }
                #endregion
                #region [ キー入力: 前に移動 ]
                ctキー反復用.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow), new CCounter.DGキー処理(tMoveToPrevious));
                ctキー反復用.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.R), new CCounter.DGキー処理(tMoveToPrevious));
                //Change to HT
                if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
                {
                    tMoveToPrevious();
                }
                #endregion
                #region [ キー入力: 次に移動 ]
                ctキー反復用.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow), new CCounter.DGキー処理(tMoveToNext));
                ctキー反復用.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.G), new CCounter.DGキー処理(tMoveToNext));
                //Change to LT
                if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
                {
                    tMoveToNext();
                }
                #endregion
            }
                
            cursor.position.Y = 6 + (32 * (nCurrentSelection + 1));
                
            //draw value items
            for (int i = 0; i < lciMenuItems.Count; i++)
            {
                bool bItemBold = i == nCurrentSelection;
                
                var pair = listItems[i];
                string s;
                switch (lciMenuItems[i].strItemName)
                {
                    case "PlaySpeed":
                    {
                        double d = (double)((int)lciMenuItems[i].GetCurrentValue() / 20.0);
                        s = "x" + d.ToString("0.000");
                    }
                        break;
                    case "ScrollSpeed":
                    {
                        double d = (double)((((int)lciMenuItems[i].GetCurrentValue()) + 1) / 2.0);
                        s = "x" + d.ToString("0.0");
                    }
                        break;

                    default:
                        s = lciMenuItems[i].GetCurrentValue().ToString();
                        break;
                }

                bool bValueBold = i == nCurrentSelection && bIsSelectingIntItem;
                pair.value.SetText(s);
                pair.value.isHighlighted = bValueBold;
            }
            
            tDrawSub();
            ui.isVisible = true;
        }
        else
        {
            ui.isVisible = false;
        }
        return 0;
    }


    // Other

    #region [ private ]
    //-----------------
    protected UIGroup ui;
    private UIGroup menuItems;
    private UIImage cursor;

    private bool bキー入力待ち;

    internal int nCurrentSelection;
    internal EInstrumentPart eInst = EInstrumentPart.UNKNOWN;
        
    private CActDFPFont font;

    private string strMenuTitle;
    private List<CItemBase> lciMenuItems;
    private bool bIsSelectingIntItem;
    public CStageSongSelection.CCommandHistory CommandHistory;

    [StructLayout(LayoutKind.Sequential)]
    private struct STキー反復用カウンタ
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
    private STキー反復用カウンタ ctキー反復用;

    private enum ESortAction : int
    {
        Decide, END
    }
    //-----------------
    #endregion
}