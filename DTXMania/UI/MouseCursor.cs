using DTXMania.Core;
using DTXMania.Core.Framework;
using DTXMania.UI.Inspector;

namespace DTXMania.UI;

public static class MouseCursor
{
    private static bool clickedIntoGame;

    public static void Update(BaseGame game)
    {
        if (!game.isFocused)
        {
            clickedIntoGame = false;
        }
        else if (PointerInput.leftPressed)
        {
            clickedIntoGame = true;
        }

        bool hidden = game.isFocused
                      && !InspectorManager.WantsImGui
                      && HidesIn(game.host.fullscreenMode);

        game.host.SetCursorVisible(!hidden);
    }

    private static bool HidesIn(FullscreenMode fullscreenMode) => CDTXMania.ConfigIni.eHideCursor switch
    {
        CConfigIni.HideCursor.Fullscreen => fullscreenMode != FullscreenMode.Windowed,
        CConfigIni.HideCursor.Always => fullscreenMode != FullscreenMode.Windowed || clickedIntoGame,
        _ => false
    };
}
