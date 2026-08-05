using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Config;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Item;
using DTXMania.UI.Text;
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

    public override void RegisterBindings()
    {
    }

    //the config screen is all bespoke interactive panels, so it is built in code rather than as a layout
    public override void OnLayoutReady()
    {
        //left menu
        UIGroup leftMenu = ui.AddChild(new UIGroup("Left Options Menu"));
        leftMenu.position = new Vector3(245, 140, 0);
        leftMenu.renderOrder = 30;
        leftMenu.dontSerialize = true;
        
        UIImage menuPanel = leftMenu.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_menu panel.png"))));
        menuPanel.position = Vector3.Zero;
            
        //menu items
        configLeftOptionsMenu = leftMenu.AddChild(new UIMenu("Button List"));
        configLeftOptionsMenu.dontSerialize = true;
        configLeftOptionsMenu.itemOffset = new Vector3(0, 32, 0);
        configLeftOptionsMenu.itemComponent = "Components/ConfigMenuButton.json";
        configLeftOptionsMenu.itemDefault = BuildMenuButtonDefault;

        //340 - size/2, so this becomes 340-245= 95
        configLeftOptionsMenu.position = new Vector3(95, 6, 0);

        //todo: render menu cursor correctly to match current version of the game. right now its rendered as a stretched image.
        menuCursor = configLeftOptionsMenu.AddChild(new UIImage(BaseTexture.LoadFromPath(CSkin.Path(@"Graphics\4_menu cursor.png"))));
        menuCursor.position = new Vector3(-5, 0, 0);
        menuCursor.size = new Vector2(170, 28);
        menuCursor.anchor = new Vector2(0.5f, 0f);
        menuCursor.renderMode = ERenderMode.Sliced;
        menuCursor.sliceRect = new RectangleF(16, 0, 32, 28);
        menuCursor.bindings.Add(new UIBinding("position.Y", "Selection.Y"));

        configList = ui.AddChild(new ConfigList(14, 4));
        configList.position = new Vector3(420, 189, 0);
        configList.renderOrder = 41;
        configList.isVisible = true;
        configList.dontSerialize = true;
        
        //at the root of a page, Cancel hands focus back to the left menu
        configList.onExitRoot = () => UIFocus.Pop(configList);

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
        keyAssignPanel.onNext = KeyAssignNext;
        keyAssignPanel.isVisible = false;

        inputTestPanel = ui.AddChild(new InputTestPanel());
        inputTestPanel.position = new Vector3(450, 120, 0);
        inputTestPanel.renderOrder = 42;
        inputTestPanel.onClose = CloseKeyAssign;
        inputTestPanel.isVisible = false;

        midiTestPanel = ui.AddChild(new MidiTestPanel());
        midiTestPanel.position = new Vector3(450, 120, 0);
        midiTestPanel.renderOrder = 42;
        midiTestPanel.onClose = CloseKeyAssign;
        midiTestPanel.isVisible = false;

        configList.onOpenKeyAssign = OpenKeyAssign;
        configList.onOpenInputTest = OpenInputTest;
        configList.onOpenMidiTest = OpenMidiTest;
        
        //moving through the categories loads them; choosing one drops focus into its page
        configLeftOptionsMenu.SetEntries([
            new UIMenuItem("System", configMenu.OpenSystem),
            new UIMenuItem("Drums", configMenu.OpenDrums),
            new UIMenuItem("Guitar P1", configMenu.OpenGuitar),
            new UIMenuItem("Guitar P2", configMenu.OpenBass),
            new UIMenuItem("Exit", string.Empty)
        ]);

        configLeftOptionsMenu.onSelectionChanged = OpenCategory;
        configLeftOptionsMenu.onDecide = EnterCategory;
        configLeftOptionsMenu.onCancel = StartExitConfig;
        focusTarget = configLeftOptionsMenu;
    }

    //what one left-menu button looks like: white, or a yellow-to-orange gradient while it is selected
    private static UIGroup BuildMenuButtonDefault()
    {
        UIGroup root = new("MenuButton");

        UIText label = root.AddChild(new UIText(string.Empty, 20));
        label.name = "Label";
        label.anchor = new Vector2(0.5f, 0f);
        label.position = new Vector3(-5, 0, 0);
        label.bindings.Add(new UIBinding("text", "Item.Label"));
        label.bindings.Add(new UIBinding("isVisible", "IsSelected") { invert = true });

        UIText selected = root.AddChild(new UIText(string.Empty, 20));
        selected.name = "LabelSelected";
        selected.anchor = new Vector2(0.5f, 0f);
        selected.position = new Vector3(-5, 0, 0);
        selected.bindings.Add(new UIBinding("text", "Item.Label"));
        selected.fillGradientMode = UiTextGradientMode.Vertical;
        selected.fillGradientTopColor = new Color4(1f, 1f, 0f);
        selected.fillGradientBottomColor = new Color4(1f, 0.27f, 0f);
        selected.bindings.Add(new UIBinding("isVisible", "IsSelected"));

        return root;
    }

    private void OpenCategory(UIMenuItem entry)
    {
        ctDisplayWait.nCurrentValue = 0;
        entry.Run?.Invoke();
    }

    private void EnterCategory(UIMenuItem entry)
    {
        if (configLeftOptionsMenu.SelectedItem == MenuExitIndex)
        {
            StartExitConfig();
            return;
        }

        UIFocus.Push(configList);
    }

    public override void BuildDefaultLayout()
    {
        ui.AddChild(new UIImage
        {
            name = "Background",
            imageSource = ImageSource.System,
            resource = @"Graphics\4_background.png",
            renderOrder = -100
        });

        ui.AddChild(new UIImage
        {
            name = "ItemBar",
            imageSource = ImageSource.System,
            resource = @"Graphics\4_item bar.png",
            position = new Vector3(400, 0, 0),
            renderOrder = 20
        });

        ui.AddChild(new UIImage
        {
            name = "HeaderPanel",
            imageSource = ImageSource.System,
            resource = @"Graphics\4_header panel.png",
            renderOrder = 52
        });

        //anchored to its own bottom edge, since the texture's height is not known until it loads
        ui.AddChild(new UIImage
        {
            name = "FooterPanel",
            imageSource = ImageSource.System,
            resource = @"Graphics\4_footer panel.png",
            anchor = new Vector2(0, 1),
            position = new Vector3(0, 720, 0),
            renderOrder = 53
        });
    }

    public override void OnActivate()
    {
        Trace.TraceInformation("コンフィグステージを活性化します。");
        Trace.Indent();
        try
        {
            ctDisplayWait = new CCounter( 0, 350, 1, CDTXMania.Timer );
        }
        finally
        {
            Trace.TraceInformation("コンフィグステージの活性化を完了しました。");
            Trace.Unindent();
        }
        base.OnActivate();		// 2011.3.14 yyagi: OnActivate()をtryの中から外に移動

        if (configLeftOptionsMenu != null)
        {
            configLeftOptionsMenu.SelectedItem = 0;
        }
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

    private UIMenu configLeftOptionsMenu;
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

        //the cursor follows the selection through a binding; only its dimming is about focus
        menuCursor.color.Alpha = UIFocus.Holds(configLeftOptionsMenu) ? 1.0f : 0.5f;

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

        descriptionPanel.Update(configList.CurrentItem, configList.IsActive && configList.IsSettled);
        return 0;
    }
    private ConfigList configList;
    private ConfigDescriptionPanel descriptionPanel;
    private ConfigMenu configMenu;
    private KeyAssignPanel keyAssignPanel; //key-assign editor overlay (opened from a pad-list row)
    private InputTestPanel inputTestPanel;  //all-channel input-test overview (opened from an "Input Test" row)
    private MidiTestPanel midiTestPanel;    //MIDI diagnostics feed (opened from the drums "MIDI Test" row)

    private const int MenuExitIndex = 4;

    public CCounter ctDisplayWait;

    private void StartExitConfig()
    {
        //nothing here reads input once the stage starts leaving
        UIFocus.Pop(configLeftOptionsMenu);

        GitaDoraTransition.Close(0, async () =>
        {
            await Task.Delay(50);
            GitaDoraTransition.Open();
        });
        ePhaseID = EPhase.Common_FadeOut;
    }

    //opens the key-assign editor for a pad and hands input over to it (called back from a pad row)
    private void OpenKeyAssign(EKeyConfigPart part, EKeyConfigPad pad, string padName)
    {
        configList.isVisible = false;
        descriptionPanel.Update(null, false);
        keyAssignPanel.Open(part, pad, padName);
    }

    private void OpenInputTest((EKeyConfigPart, EKeyConfigPad, string)[] pads)
    {
        configList.isVisible = false;
        descriptionPanel.Update(null, false);
        inputTestPanel.Open(pads);
    }

    private void OpenMidiTest((EKeyConfigPart, EKeyConfigPad, string)[] pads)
    {
        configList.isVisible = false;
        descriptionPanel.Update(null, false);
        midiTestPanel.Open(pads);
    }

    //the panels pop themselves, so the page they were opened from is focused again
    private void CloseKeyAssign()
    {
        configList.isVisible = true;
    }

    private void KeyAssignNext()
    {
        CItemBase? next = configList.SelectNextNormal();
        if (next is { ePanelType: CItemBase.EPanelType.Normal })
        {
            next.RunAction(); // the pad row's action re-opens the panel for that pad
        }
    }

}