using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.Item;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageConfig : CStage
{
    public CActDFPFont actFont { get; private set; }
    
    public CStageConfig()
    {
        CActDFPFont font;
        eStageID = EStage.Config_3;
        ePhaseID = EPhase.Common_DefaultState;
        actFont = font = new CActDFPFont();
        listChildActivities.Add(font);
        listChildActivities.Add(actList = new CActConfigList(this, ui));
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
        leftMenu.renderOrder = 30;
        leftMenu.dontSerialize = true;
        
        UIImage menuPanel = leftMenu.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_menu panel.png"))));
        menuPanel.position = Vector3.Zero;
            
        //menu items
        configLeftOptionsMenu = leftMenu.AddChild(new UISelectList("Button List"));
        configLeftOptionsMenu.isVisible = true;
        configLeftOptionsMenu.dontSerialize = true;
        
        //340 - size/2, so this becomes 340-245= 95
        configLeftOptionsMenu.position = new Vector3(95, 4, 0);

        //todo: render menu cursor correctly to match current version of the game. right now its rendered as a stretched image.
        menuCursor = configLeftOptionsMenu.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_menu cursor.png"))));
        menuCursor.position = new Vector3(-5, 2, 0);
        menuCursor.size = new Vector2(170, 28);
        menuCursor.anchor = new Vector2(0.5f, 0f);
        menuCursor.renderMode = ERenderMode.Sliced;
        menuCursor.sliceRect = new RectangleF(16, 0, 32, 28);

        // menu buttons dispatch to whichever backend is active (old CActConfigList or the new ConfigList)
        var family = new FontFamily(CDTXMania.ConfigIni.songListFont);
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 20, "System",
            () => { if (useNewList) configMenu.OpenSystem(); else actList.tSetupItemList_System(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 20, "Drums",
            () => { if (useNewList) configMenu.OpenDrums(); else actList.tSetupItemList_Drums(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 20, "Guitar P1",
            () => { if (useNewList) configMenu.OpenGuitar(); else actList.tSetupItemList_Guitar(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 20, "Guitar P2",
            () => { if (useNewList) configMenu.OpenBass(); else actList.tSetupItemList_Bass(); }));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(family, 20, "Exit",
            () => { if (!useNewList) actList.tSetupItemList_Exit(); }));
        configLeftOptionsMenu.UpdateLayout();
        configLeftOptionsMenu.SetSelectedIndex(0);

        var drawList = ui.AddChild(new LegacyDrawable(() =>
        {
            if (useNewList) return; // the new ConfigList draws itself as a normal UI element

            switch (eItemPanelMode)
            {
                case EItemPanelMode.PadList:
                    actList.tUpdateAndDraw(!bFocusIsOnMenu);
                    break;

                case EItemPanelMode.KeyCodeList:
                    actKeyAssign.OnUpdateAndDraw();
                    break;
            }
        }));
        drawList.renderOrder = 40;
        
        descriptionPanel = ui.AddChild(new UIText("", 17));
        descriptionPanel.name = "DescriptionPanel";
        descriptionPanel.position = new Vector3(800, 270, 0);
        descriptionPanel.renderOrder = 50;
        descriptionPanel.isVisible = false;
        
        if (bFocusIsOnMenu)
        {
            tDrawSelectedMenuDescriptionInDescriptionPanel();
        }
        else
        {
            tDrawSelectedItemDescriptionInDescriptionPanel();
        }

        #region [ Experimental new config list (F1 to toggle) ]

        newConfigList = ui.AddChild(new ConfigList(14, 4));
        newConfigList.position = new Vector3(420, 189, 0);
        newConfigList.renderOrder = 41;
        newConfigList.isVisible = true;
        newConfigList.dontSerialize = true;
        
        //at the root of a page, Cancel returns focus to the left menu
        newConfigList.onExitRoot = () => bFocusIsOnMenu = true;

        //description panel (background + text) for the new config list
        newDescriptionPanel = ui.AddChild(new ConfigDescriptionPanel());
        newDescriptionPanel.position = new Vector3(781, 252, 0);
        newDescriptionPanel.renderOrder = 49;

        configMenu = new ConfigMenu(newConfigList);
        configMenu.OpenSystem(); //seed a page so the list has content before it's first shown

        //key-assign editor overlay: hidden until a pad row opens it; drawn just above the list
        keyAssignPanel = ui.AddChild(new KeyAssignPanel());
        keyAssignPanel.position = new Vector3(450, 120, 0);
        keyAssignPanel.renderOrder = 42;
        keyAssignPanel.onClose = CloseKeyAssign;
        keyAssignPanel.isVisible = false;
        newConfigList.onOpenKeyAssign = OpenKeyAssign;

        useNewList = true;
        usedNewList = true;

        #endregion
    }

    public override void InitializeDefaultUI()
    {
        //create resources for menu elements
        UIImage bg = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_background.png"))));
        bg.renderOrder = -100;
        bg.position = Vector3.Zero;
                
        UIImage itemBar = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_item bar.png"))));
        itemBar.position = new Vector3(400, 0, 0);
        itemBar.renderOrder = 20;
                
        UIImage headerPanel = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_header panel.png"))));
        headerPanel.position = Vector3.Zero;
        headerPanel.renderOrder = 52;
                
        UIImage footerPanel = ui.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_footer panel.png"))));
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

            // apply deferred sound-device changes made via the new list (gated so it doesn't
            // double-apply with the old CActConfigList path when only one was used)
            if (usedNewList)
            {
                configMenu?.ApplyPendingChanges();
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
    private UIText descriptionPanel;

    public override void FirstUpdate()
    {
        ePhaseID = EPhase.Common_FadeIn;

        GitaDoraTransition.Open(2, () =>
        {
            CDTXMania.Skin.bgmコンフィグ画面.tPlay();
            ePhaseID = EPhase.Common_DefaultState;
        });
    }

    public override int OnUpdateAndDraw()
    {
        if (!bActivated) return 0;

        base.OnUpdateAndDraw();

        ctDisplayWait.tUpdate();

        //update menu cursor position
        menuCursor.color.Alpha = bFocusIsOnMenu ? 1.0f : 0.5f;
        menuCursor.position.Y = 2 + configLeftOptionsMenu.currentlySelectedIndex * 32;

        #region [ Description panel ]

        //--------------------- (the new list manages its own description in HandleNewConfigListInput)
        if (!useNewList)
        {
            if (!bFocusIsOnMenu && actList.nTargetScrollCounter == 0 &&
                ctDisplayWait.bReachedEndValue)
            {
                descriptionPanel.isVisible = true;
            }
            else
            {
                descriptionPanel.isVisible = false;
            }
        }

        #endregion

        #region [ Fade in and out ]

        //---------------------
        switch (ePhaseID)
        {
            case EPhase.Common_FadeIn:
                CDTXMania.Skin.bgmコンフィグ画面.tPlay();
                ePhaseID = EPhase.Common_DefaultState;
                break;

            case EPhase.Common_FadeOut:
                if (GitaDoraTransition.isAnimating) break;
                return 1;
        }

        //---------------------

        #endregion

        // キー入力

        if ((ePhaseID != EPhase.Common_DefaultState) || actKeyAssign.bWaitingForKeyInput)
            return 0;

        // F1 toggles the experimental new config list (not while the key-assign editor owns input)
        if (!keyAssignPanel.IsOpen && CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.F1))
        {
            SetUseNewList(!useNewList);
        }

        if (useNewList)
        {
            HandleNewConfigListInput();
            return 0;
        }

        // 曲データの一覧取得中は、キー入力を無効化する
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

                if (!actList.bIsSubMenuSelected &&
                    !actList.bIsFocusingParameter) // #24525 2011.3.15 yyagi, #32059 2013.9.17 yyagi
                {
                    bFocusIsOnMenu = true;
                }

                tDrawSelectedMenuDescriptionInDescriptionPanel();
                actList.tPressEsc(); // #24525 2011.3.15 yyagi ESC押下時の右メニュー描画用
            }
            else
            {
                GitaDoraTransition.Close(0, async () =>
                {
                    await Task.Delay(50);
                    GitaDoraTransition.Open();
                });
                ePhaseID = EPhase.Common_FadeOut;
            }
        }
        else if (CDTXMania.Input.ActionDecide())
        {
            if (configLeftOptionsMenu.currentlySelectedIndex == 4)
            {
                CDTXMania.Skin.soundDecide.tPlay();
                GitaDoraTransition.Close(0, async () =>
                {
                    await Task.Delay(50);
                    GitaDoraTransition.Open();
                });
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
                        bool bIsKeyAssignSelectedBeforeHitEnter = actList.bIsSubMenuSelected; // #24525 2011.3.15 yyagi
                        actList.tPressEnter();
                        if (actList.bCurrentlySelectedItemIsReturnToMenu)
                        {
                            tDrawSelectedMenuDescriptionInDescriptionPanel();
                            if (bIsKeyAssignSelectedBeforeHitEnter == false) // #24525 2011.3.15 yyagi
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

        ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
            tMoveCursorUp);
        ctKeyRepetition.R.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.HH), tMoveCursorUp);
        //Change to HT
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.HT))
        {
            tMoveCursorUp();
        }

        ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
            tMoveCursorDown);
        ctKeyRepetition.B.tRepeatKey(CDTXMania.Pad.bPressingGB(EPad.SD), tMoveCursorDown);
        //Change to LT
        if (CDTXMania.Pad.bPressed(EInstrumentPart.DRUMS, EPad.LT))
        {
            tMoveCursorDown();
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
    public struct STKeyRepetitionCounter
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

    private CActConfigKeyAssign actKeyAssign;
    private CActConfigList actList;

    // --- experimental new config list (toggled with F1) ---
    private ConfigList newConfigList;
    private ConfigDescriptionPanel newDescriptionPanel;
    private ConfigMenu configMenu;
    private KeyAssignPanel keyAssignPanel; // key-assign editor overlay (opened from a pad-list row)
    private bool useNewList;
    private bool usedNewList; // whether the new list was ever shown this visit (gates device apply)

    private const int MenuExitIndex = 4;

    private bool bFocusIsOnMenu;
    private STKeyRepetitionCounter ctKeyRepetition;
    private EItemPanelMode eItemPanelMode;
        
    public CCounter ctDisplayWait;
        
    private void SetUseNewList(bool value)
    {
        useNewList = value;
        newConfigList.isVisible = value;
        if (value) usedNewList = true;

        // hand the description panel over to the active backend (avoid both showing at once)
        if (value) descriptionPanel.isVisible = false; // old backend's text element
        else newDescriptionPanel.Update(null, false);  // new backend's panel

        // the active backend owns config; stop the inactive old list from writing stale values on exit
        actList.suppressConfigWrite = value;

        // reset to the menu and (re)load the current category into whichever backend is now active
        bFocusIsOnMenu = true;
        configLeftOptionsMenu.RunAction();
    }

    private void StartExitConfig()
    {
        GitaDoraTransition.Close(0, async () =>
        {
            await Task.Delay(50);
            GitaDoraTransition.Open();
        });
        ePhaseID = EPhase.Common_FadeOut;
    }

    private void MoveMenuSelection(bool next)
    {
        CDTXMania.Skin.soundCursorMovement.tPlay();
        ctDisplayWait.nCurrentValue = 0;

        if (next) configLeftOptionsMenu.SelectNext();
        else configLeftOptionsMenu.SelectPrevious();

        configLeftOptionsMenu.RunAction(); // loads the newly-selected category into the new list
    }

    // input handling while the new ConfigList is active (mirrors the old menu/list focus flow)
    private void HandleNewConfigListInput()
    {
        // the key-assign editor, when open, owns all input until it closes
        if (keyAssignPanel.IsOpen)
        {
            HandleKeyAssignInput();
            return;
        }

        // cursor + scroll arrows belong to the list; hide them while the left menu has focus
        newConfigList.SetFocused(!bFocusIsOnMenu);

        if (bFocusIsOnMenu)
        {
            if (CDTXMania.Input.ActionCancel())
            {
                CDTXMania.Skin.soundCancel.tPlay();
                StartExitConfig();
            }
            else if (CDTXMania.Input.ActionDecide())
            {
                CDTXMania.Skin.soundDecide.tPlay();
                if (configLeftOptionsMenu.currentlySelectedIndex == MenuExitIndex)
                {
                    StartExitConfig();
                }
                else
                {
                    bFocusIsOnMenu = false; // drop focus into the list page
                }
            }

            ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
                () => MoveMenuSelection(false));
            ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
                () => MoveMenuSelection(true));
        }
        else
        {
            if (CDTXMania.Input.ActionCancel())
            {
                CDTXMania.Skin.soundCancel.tPlay();
                newConfigList.Cancel(); // pops a folder, or at the root returns focus to the menu
            }
            else if (CDTXMania.Input.ActionDecide())
            {
                newConfigList.Confirm();
            }

            ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
                () => newConfigList.MoveUp());
            ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
                () => newConfigList.MoveDown());
        }

        // the description panel only shows once a page is focused and fully aligned
        bool showDescription = !bFocusIsOnMenu && newConfigList.IsSettled && !keyAssignPanel.IsOpen;
        newDescriptionPanel.Update(newConfigList.CurrentItem, showDescription);
    }

    // Opens the key-assign editor for a pad and hands input over to it (called back from a pad row).
    private void OpenKeyAssign(EKeyConfigPart part, EKeyConfigPad pad, string padName)
    {
        newConfigList.isVisible = false;
        newConfigList.SetFocused(false);
        newDescriptionPanel.Update(null, false);
        keyAssignPanel.Open(part, pad, padName);
    }

    // Returns from the editor to the pad list it was opened from.
    private void CloseKeyAssign()
    {
        newConfigList.isVisible = true;
        bFocusIsOnMenu = false; // focus stays on the (pad list) page, not the left menu
    }

    // Input while the key-assign editor is open. PollCapture runs every frame so it can grab the next
    // input while waiting; otherwise Up/Down/Decide/Cancel/Delete drive the editor.
    private void HandleKeyAssignInput()
    {
        keyAssignPanel.PollCapture();

        if (keyAssignPanel.IsWaiting)
        {
            return; // capture owns input (Esc-to-cancel handled inside PollCapture)
        }

        if (CDTXMania.Input.ActionCancel())
        {
            keyAssignPanel.Cancel();
        }
        else if (CDTXMania.Input.ActionDecide())
        {
            keyAssignPanel.Confirm();
        }
        else if (CDTXMania.InputManager.Keyboard.bKeyPressed(SlimDXKey.Delete))
        {
            keyAssignPanel.DeleteCurrent();
        }

        ctKeyRepetition.Up.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.UpArrow),
            () => keyAssignPanel.MoveUp());
        ctKeyRepetition.Down.tRepeatKey(CDTXMania.InputManager.Keyboard.bKeyPressing(SlimDXKey.DownArrow),
            () => keyAssignPanel.MoveDown());
    }

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
        string explanation = "";
        
        switch (configLeftOptionsMenu.currentlySelectedIndex)
        {
            case 0: //system
                explanation = CDTXMania.isJapanese 
                    ? "システムに関係する項目を設定します。" 
                    : "Settings for an overall systems.";
                break;

            case 1: //drums
                explanation = CDTXMania.isJapanese
                    ? "ドラムの演奏に関する項目を設定します。"
                    : "Settings to play the drums.";
                break;

            case 2: //guitar
                explanation = CDTXMania.isJapanese
                    ? "ギターの演奏に関する項目を設定します。"
                    : "Settings to play the guitar.";
                break;

            case 3: //bass
                explanation = CDTXMania.isJapanese
                    ? "ベースの演奏に関する項目を設定します。"
                    : "Settings to play the bass.";
                break;

            case 4: //exit
                explanation = CDTXMania.isJapanese
                    ? "設定を保存し、コンフィグ画面を終了します。"
                    : "Save the settings and exit from\nCONFIGURATION menu.";
                break;
        }
        
        descriptionPanel.SetText(explanation);
    }
    private void tDrawSelectedItemDescriptionInDescriptionPanel()
    {
        CItemBase item = actList.ibCurrentSelection;
        if (item.strDescription is {Length: > 0})
        {
            descriptionPanel.SetText(item.strDescription);
        }
    }
    #endregion
}