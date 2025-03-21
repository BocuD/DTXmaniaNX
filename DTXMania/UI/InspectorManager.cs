﻿using DTXMania.Core;
using DTXUIRenderer;
using SlimDX.DirectInput;

namespace DTXMania.UI;

public static class InspectorManager
{
    public static Inspector inspector { get; private set; }
    public static HierarchyWindow hierarchyWindow { get; private set; }

    private static bool inspectorEnabled = false;
    
    static InspectorManager()
    {
        inspector = new Inspector();
        hierarchyWindow = new HierarchyWindow();
    }
    
    public static void Draw()
    {
        if (CDTXMania.InputManager.Keyboard.bKeyPressing(Key.LeftControl) 
            && CDTXMania.InputManager.Keyboard.bKeyPressed(Key.I))
        {
            inspectorEnabled = !inspectorEnabled;
        }
        
        if (inspectorEnabled)
        {
            inspector.Draw();
            hierarchyWindow.Draw();
        }
    }
}