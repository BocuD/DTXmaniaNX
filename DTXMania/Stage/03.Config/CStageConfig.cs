using System.Runtime.InteropServices;
using DTXMania.UI.Skin;
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

    protected override StageRoot CreateRoot() => new() { canvasFit = UiCanvasFit.Fill };

    public override void RegisterBindings()
    {
    }

    public override void OnLayoutReady()
    {
        configLeftOptionsMenu = ui.FindChild<UIMenu>();
        configList = ui.FindChild<ConfigList>();
        descriptionPanel = ui.FindChild<ConfigDescriptionPanel>();
        menuCursor = configLeftOptionsMenu?.GetChild<UIImage>("MenuCursor");

        //a layout is free to leave either of them out, and then there is nothing here to drive
        if (configList == null || configLeftOptionsMenu == null)
        {
            return;
        }

        //at the root of a page, Cancel hands focus back to the left menu
        configList.onExitRoot = () => UIFocus.Pop(configList);

        //whatever the open page puts beside the list, in screen space and cleared with the page
        UIGroup pageElements = ui.AddChild(new UIGroup("Page Elements"));
        pageElements.renderOrder = 49;
        pageElements.dontSerialize = true;
        configList.pageElements = pageElements;

        configMenu = new ConfigMenu(configList);
        configMenu.OpenSystem(); //seed a page so the list has content before it's first shown

        //key-assign editor overlay: hidden until a pad row opens it; drawn just above the list
        keyAssignPanel = ui.AddChild(new KeyAssignPanel());
        keyAssignPanel.parentAnchor = UICanvas.Center;
        keyAssignPanel.position = UICanvas.FromCenter(450, 120);
        keyAssignPanel.renderOrder = 42;
        keyAssignPanel.onClose = CloseKeyAssign;
        keyAssignPanel.onNext = KeyAssignNext;
        keyAssignPanel.isVisible = false;

        inputTestPanel = ui.AddChild(new InputTestPanel());
        inputTestPanel.parentAnchor = UICanvas.Center;
        inputTestPanel.position = UICanvas.FromCenter(450, 120);
        inputTestPanel.renderOrder = 42;
        inputTestPanel.onClose = CloseKeyAssign;
        inputTestPanel.isVisible = false;

        midiTestPanel = ui.AddChild(new MidiTestPanel());
        midiTestPanel.parentAnchor = UICanvas.Center;
        midiTestPanel.position = UICanvas.FromCenter(450, 120);
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
    private static UIGroup ConfigMenuButton()
    {
        UIGroup root = new("MenuButton");

        UIText label = root.AddChild(new UIText(string.Empty, 20));
        label.name = "Label";
        label.pivot = new Vector2(0.5f, 0f);
        label.position = new Vector3(-5, 0, 0);
        label.bindings.Add(new UIBinding("text", "Item.Label"));
        label.bindings.Add(new UIBinding("isVisible", "IsSelected") { invert = true });

        UIText selected = root.AddChild(new UIText(string.Empty, 20));
        selected.name = "LabelSelected";
        selected.pivot = new Vector2(0.5f, 0f);
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
        if (configLeftOptionsMenu == null || configList == null)
        {
            return;
        }

        if (configLeftOptionsMenu.SelectedItem == MenuExitIndex)
        {
            StartExitConfig();
            return;
        }

        UIFocus.Push(configList);
    }

    public override void BuildDefaultLayout()
    {
        UICoverGroup background = ui.AddChild(new UICoverGroup("Background"));
        background.renderOrder = -100;
        background.AddChild(new UIImage
        {
            name = "Image",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_background.png"),
            size = UISize.Inherited
        });

        ui.AddChild(new UIImage
        {
            name = "ItemBar",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_item bar.png"),
            parentAnchor = new Vector2(0.5f, 0f),
            position = UICanvas.FromCenter(400, 0) with { Y = 0 },
            size = new UISize { yMode = UiSizeMode.Inherit },
            renderOrder = 20
        });

        ui.AddChild(new UIImage
        {
            name = "HeaderPanel",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_header panel.png"),
            parentAnchor = new Vector2(0.5f, 0f),
            pivot = new Vector2(0.5f, 0f),
            renderOrder = 52
        });

        //anchored to its own bottom edge, since the texture's height is not known until it loads
        ui.AddChild(new UIImage
        {
            name = "FooterPanel",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_footer panel.png"),
            parentAnchor = new Vector2(0.5f, 1f),
            pivot = new Vector2(0.5f, 1f),
            renderOrder = 53
        });

        UIGroup leftMenu = ui.AddChild(new UIGroup("Left Options Menu"));
        leftMenu.parentAnchor = UICanvas.Center;
        leftMenu.position = UICanvas.FromCenter(245, 140);
        leftMenu.renderOrder = 30;

        leftMenu.AddChild(new UIImage
        {
            name = "MenuPanel",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_menu panel.png"),
            size = new Vector2(180, 172)
        });

        UIMenu menu = leftMenu.AddChild(new UIMenu("Button List"));
        menu.itemOffset = new Vector3(0, 32, 0);
        menu.itemComponentSource = ConfigMenuButton;

        //340 - size/2, so this becomes 340-245= 95
        menu.position = new Vector3(95, 6, 0);

        //todo: render menu cursor correctly to match current version of the game. right now its rendered as a stretched image.
        menu.AddChild(new UIImage
        {
            name = "MenuCursor",
            imageSource = ImageSource.File,
            image = SkinResource.System(@"Graphics\4_menu cursor.png"),
            position = new Vector3(-5, 0, 0),
            size = new Vector2(170, 28),
            pivot = new Vector2(0.5f, 0f),
            renderMode = ERenderMode.Sliced,
            sliceRect = new RectangleF(16, 0, 32, 28),
            bindings = { new UIBinding("position.Y", "Selection.Y") }
        });

        ConfigList list = ui.AddChild(new ConfigList(25, 11));
        list.parentAnchor = UICanvas.Center;
        list.position = UICanvas.FromCenter(420, 189);
        list.renderOrder = 41;

        ConfigDescriptionPanel description = ui.AddChild(new ConfigDescriptionPanel());
        description.parentAnchor = UICanvas.Center;
        description.position = UICanvas.FromCenter(781, 252);
        description.renderOrder = 49;

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
            configMenu?.ApplyPendingChanges();

            //the open page's own elements go with the stage rather than outliving it
            configList?.ClosePage();

            //the config BGM is this stage's; it has no business staying resident through a song
            CDTXMania.Skin.bgmコンフィグ画面.Unload();

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

    private UIMenu? configLeftOptionsMenu;
    private UIImage? menuCursor;

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
        if (menuCursor != null && configLeftOptionsMenu != null)
        {
            menuCursor.color.Alpha = UIFocus.Holds(configLeftOptionsMenu) ? 1.0f : 0.5f;
        }

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

        if (configList != null)
        {
            descriptionPanel?.Update(configList.CurrentItem, configList.IsActive && configList.IsSettled);
        }

        return 0;
    }
    private ConfigList? configList;
    private ConfigDescriptionPanel? descriptionPanel;
    private ConfigMenu? configMenu;
    private KeyAssignPanel keyAssignPanel; //key-assign editor overlay (opened from a pad-list row)
    private InputTestPanel inputTestPanel;  //all-channel input-test overview (opened from an "Input Test" row)
    private MidiTestPanel midiTestPanel;    //MIDI diagnostics feed (opened from the drums "MIDI Test" row)

    private const int MenuExitIndex = 4;

    public CCounter ctDisplayWait;

    private void StartExitConfig()
    {
        //the change out of here is dropped in preview, which would leave the stage fading out forever
        if (previewMode)
        {
            return;
        }

        //nothing here reads input once the stage starts leaving
        if (configLeftOptionsMenu != null)
        {
            UIFocus.Pop(configLeftOptionsMenu);
        }

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
        HideListForPanel();
        keyAssignPanel.Open(part, pad, padName);
    }

    private void OpenInputTest((EKeyConfigPart, EKeyConfigPad, string)[] pads)
    {
        HideListForPanel();
        inputTestPanel.Open(pads);
    }

    private void OpenMidiTest((EKeyConfigPart, EKeyConfigPad, string)[] pads)
    {
        HideListForPanel();
        midiTestPanel.Open(pads);
    }

    //a panel covers the list, so the list steps aside while one is open
    private void HideListForPanel()
    {
        if (configList != null)
        {
            configList.isVisible = false;
        }

        descriptionPanel?.Update(null, false);
    }

    //the panels pop themselves, so the page they were opened from is focused again
    private void CloseKeyAssign()
    {
        if (configList == null)
        {
            return;
        }

        configList.isVisible = true;

        //a pad row shows the mapping the panel has just been editing
        configList.RefreshValues();
    }

    private void KeyAssignNext()
    {
        CItemBase? next = configList?.SelectNextNormal();
        if (next is { ePanelType: CItemBase.EPanelType.Normal })
        {
            next.RunAction(); // the pad row's action re-opens the panel for that pad
        }
    }

}