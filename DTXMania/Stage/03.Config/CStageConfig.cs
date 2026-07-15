using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using FDK;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

internal class CStageConfig : CStage
{
    public CStageConfig()
    {
        eStageID = EStage.Config_3;
        ePhaseID = EPhase.Common_DefaultState;
        bActivated = false;
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
        
        configList = ui.AddChild(new ConfigList(14, 4));
        configList.position = new Vector3(420, 189, 0);
        configList.renderOrder = 41;
        configList.isVisible = true;
        configList.dontSerialize = true;
        
        //at the root of a page, Cancel returns focus to the left menu
        configList.onExitRoot = () => bFocusIsOnMenu = true;

        //description panel (background + text) for the new config list
        descriptionPanel = ui.AddChild(new ConfigDescriptionPanel());
        descriptionPanel.position = new Vector3(781, 252, 0);
        descriptionPanel.renderOrder = 49;

        configMenu = new ConfigMenu(configList);
        configMenu.OpenSystem(); //seed a page so the list has content before it's first shown

        //key-assign editor overlay: hidden until a pad row opens it; drawn just above the list
        keyAssignPanel = ui.AddChild(new KeyAssignPanel());
        keyAssignPanel.position = new Vector3(450, 120, 0);
        keyAssignPanel.renderOrder = 42;
        keyAssignPanel.onClose = CloseKeyAssign;
        keyAssignPanel.isVisible = false;
        
        configList.onOpenKeyAssign = OpenKeyAssign;
        
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(20, "System", configMenu.OpenSystem));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(20, "Drums", configMenu.OpenDrums));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(20, "Guitar P1", configMenu.OpenGuitar));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(20, "Guitar P2", configMenu.OpenBass));
        configLeftOptionsMenu.AddSelectableChild(new UIBasicButton(20, "Exit", () => { }));
        configLeftOptionsMenu.UpdateLayout();
        configLeftOptionsMenu.SetSelectedIndex(0);
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
            bFocusIsOnMenu = true;
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

            //apply deferred changes made via config list when exiting the stage
            configMenu.ApplyPendingChanges();

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

        if (ePhaseID != EPhase.Common_DefaultState)
            return 0;
        
        HandleConfigListInput();
        return 0;
    }
    private ConfigList configList;
    private ConfigDescriptionPanel descriptionPanel;
    private ConfigMenu configMenu;
    private KeyAssignPanel keyAssignPanel; //key-assign editor overlay (opened from a pad-list row)

    private const int MenuExitIndex = 4;

    private bool bFocusIsOnMenu;
        
    public CCounter ctDisplayWait;

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

    //config list input handling
    private void HandleConfigListInput()
    {
        //key-assign editor, when open, owns all input until it closes
        if (keyAssignPanel.IsOpen)
        {
            HandleKeyAssignInput();
            return;
        }

        //cursor + scroll arrows belong to the list; hide them while the left menu has focus
        configList.SetFocused(!bFocusIsOnMenu);

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

            CDTXMania.Input.Navigate(() => MoveMenuSelection(false), () => MoveMenuSelection(true));
        }
        else
        {
            if (CDTXMania.Input.ActionCancel())
            {
                CDTXMania.Skin.soundCancel.tPlay();
                configList.Cancel(); //pops a folder, or at the root returns focus to the menu
            }
            else if (CDTXMania.Input.ActionDecide())
            {
                configList.Confirm();
            }

            CDTXMania.Input.Navigate(configList.MoveUp, configList.MoveDown, configList.MoveUpDrums, configList.MoveDownDrums);
        }

        //the description panel only shows once a page is focused and fully aligned
        bool showDescription = !bFocusIsOnMenu && configList.IsSettled && !keyAssignPanel.IsOpen;
        descriptionPanel.Update(configList.CurrentItem, showDescription);
    }

    //opens the key-assign editor for a pad and hands input over to it (called back from a pad row)
    private void OpenKeyAssign(EKeyConfigPart part, EKeyConfigPad pad, string padName)
    {
        configList.isVisible = false;
        configList.SetFocused(false);
        descriptionPanel.Update(null, false);
        keyAssignPanel.Open(part, pad, padName);
    }

    //returns from the editor to the pad list it was opened from
    private void CloseKeyAssign()
    {
        configList.isVisible = true;
        bFocusIsOnMenu = false; //focus stays on the (pad list) page, not the left menu
    }

    private void HandleKeyAssignInput()
    {
        keyAssignPanel.PollCapture();

        if (keyAssignPanel.IsWaiting)
        {
            return; //capture owns input (Esc-to-cancel handled inside PollCapture)
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

        CDTXMania.Input.Navigate(keyAssignPanel.MoveUp, keyAssignPanel.MoveDown);
    }
}