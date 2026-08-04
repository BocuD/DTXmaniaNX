using System.Numerics;
using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using DTXMania.UI.Skin;
using SlimDXKey = SlimDX.DirectInput.Key;

namespace DTXMania;

/// <summary>
/// Dev-only test bed for the json layout system: everything visual comes from the layout, while the
/// behaviour it binds to — button actions, a dynamic text and a dynamic image — is registered in code.
/// Esc or Cancel returns to the title.
/// </summary>
internal sealed class CStageUITest : CStage
{
    public enum EReturnValue
    {
        Continue = 0,
        ReturnToTitle = 1
    }

    private readonly UIDataContext data = new();

    private int nCounter;
    private bool returnRequested;

    private UIMenu? menu;

    private BaseTexture txToggleA = BaseTexture.None;
    private BaseTexture txToggleB = BaseTexture.None;

    public CStageUITest()
    {
        eStageID = EStage.UITest_10;
        ePhaseID = EPhase.Common_DefaultState;
        bActivated = false;
    }

    public override void RegisterBindings()
    {
        dynamicActions["AddOne"] = () => SetCounter(nCounter + 1);
        dynamicActions["Reset"] = () => SetCounter(0);
        dynamicActions["ReturnToTitle"] = () => returnRequested = true;

        data.DeclareString("Counter");
        data.RegisterTexture("ToggleImage", () => nCounter % 2 == 0 ? txToggleA : txToggleB);
        ui.dataContext = data;

        SetCounter(nCounter);
    }

    //formatted on change rather than on read: a bound element reads its source every frame
    private void SetCounter(int value)
    {
        nCounter = value;
        data.SetString("Counter", $"Counter: {nCounter}");
    }

    public override void BuildDefaultLayout()
    {
        UIGroup layout = BuildTestLayout();
        foreach (UIDrawable child in layout.children.ToList())
        {
            ui.AddChild(child);
        }
    }

    public override void OnLayoutReady()
    {
        menu = ui.children.OfType<UIMenu>().FirstOrDefault();
        if (menu == null)
        {
            return;
        }

        menu.itemDefault = BuildMenuItemDefault;
        menu.onCancel = () => returnRequested = true;
        focusTarget = menu;
        menu.SetEntries([
            new UIMenuItem("Add 1", "AddOne"),
            new UIMenuItem("Reset", "Reset"),
            new UIMenuItem("Return to title", "ReturnToTitle")
        ]);

        //the cursor is a plain element that follows the selection through the menu's own context; it has
        //no serializable form only because of the solid-colour texture
        UIImage cursor = menu.AddChild(new UIImage(BaseTexture.CreateSolidColor(new Color4(1f, 0.85f, 0.2f, 0.3f))));
        cursor.name = "MenuCursor";
        cursor.dontSerialize = true;
        cursor.anchor = new Vector2(0.5f, 0.5f);
        cursor.size = new Vector2(440, 44);
        cursor.bindings.Add(new UIBinding("position.Y", "Selection.Y"));
    }

    public override void FirstUpdate()
    {
        base.FirstUpdate();

        returnRequested = false;
    }

    public override void OnManagedCreateResources()
    {
        base.OnManagedCreateResources();

        if (bActivated)
        {
            txToggleA = BaseTexture.LoadFromPath(SkinManager.SystemPath(@"Graphics\1_background.jpg"));
            txToggleB = BaseTexture.LoadFromPath(SkinManager.SystemPath(@"Graphics\2_background.jpg"));
        }
    }

    public override void OnManagedReleaseResources()
    {
        //the textures themselves are cleaned up with the disposed ui
        txToggleA = BaseTexture.None;
        txToggleB = BaseTexture.None;
        base.OnManagedReleaseResources();
    }

    public override int OnUpdateAndDraw()
    {
        if (!bActivated) return 0;

        base.OnUpdateAndDraw();

        return returnRequested ? (int)EReturnValue.ReturnToTitle : (int)EReturnValue.Continue;
    }

    //what one menu entry looks like; everything it shows comes from the entry's own bindings
    private static UIGroup BuildMenuItemDefault()
    {
        UIGroup root = new("MenuItem");

        UIText label = root.AddChild(new UIText("", 30f));
        label.name = "Label";
        label.anchor = new Vector2(0.5f, 0.5f);
        label.bindings.Add(new UIBinding("text", "Item.Label"));

        return root;
    }

    //the in-code seed and fallback, matching what the compact json encodes
    private static UIGroup BuildTestLayout()
    {
        UIGroup root = new("UITestLayout");

        UIImage background = root.AddChild(new UIImage());
        background.name = "Background";
        background.imageSource = ImageSource.System;
        background.resource = @"Graphics\1_background.jpg";
        background.size = new Vector2(1280, 720);
        background.renderOrder = -100;

        UIText title = root.AddChild(new UIText("JSON Layout Test", 44f));
        title.name = "Title";
        title.position = new Vector3(640, 120, 0);
        title.anchor = new Vector2(0.5f, 0.5f);

        UIText counter = root.AddChild(new UIText("", 30f));
        counter.name = "CounterText";
        counter.position = new Vector3(640, 200, 0);
        counter.anchor = new Vector2(0.5f, 0.5f);
        counter.bindings.Add(new UIBinding("text", "Counter"));

        //the entries themselves are set in code; the layout only decides where the menu sits and how far
        //apart its entries are
        UIMenu menu = root.AddChild(new UIMenu("Menu"));
        menu.position = new Vector3(640, 320, 0);
        menu.itemOffset = new Vector3(0, 50, 0);
        menu.itemComponent = "Components/UITestMenuItem.json";

        UIImage toggle = root.AddChild(new UIImage());
        toggle.name = "ToggleImage";
        toggle.imageSource = ImageSource.Dynamic;
        toggle.resource = "ToggleImage";
        toggle.position = new Vector3(960, 520, 0);
        toggle.anchor = new Vector2(0.5f, 0.5f);
        toggle.size = new Vector2(300, 169);

        return root;
    }
}
