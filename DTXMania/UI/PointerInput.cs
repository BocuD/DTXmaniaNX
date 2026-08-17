using System.Numerics;
using DTXMania.UI.Inspector;
using Hexa.NET.GLFW;

namespace DTXMania.UI;

public static class PointerInput
{
    private const int LeftButton = 0;

    //a second press this far apart in time or space is a new click rather than a double one
    private const long DoubleClickMilliseconds = 400;
    private const float DoubleClickSlack = 6f;

    public static Vector2 position { get; private set; }

    public static bool leftDown { get; private set; }
    public static bool leftPressed { get; private set; }
    public static bool leftReleased { get; private set; }

    public static int clickCount { get; private set; }

    public static GlfwMod mods { get; private set; }

    public static Vector2 windowPosition { get; private set; }

    private static long lastClickTime;
    private static Vector2 lastClickPosition;

    public static void Moved(Vector2 newWindowPosition)
    {
        windowPosition = newWindowPosition;
        position = InspectorManager.WindowToGame(newWindowPosition);
    }

    public static void ButtonChanged(int button, bool down, GlfwMod newMods)
    {
        mods = newMods;

        if (button != LeftButton)
        {
            return;
        }

        leftDown = down;

        if (down)
        {
            leftPressed = true;
            CountClick();
        }
        else
        {
            leftReleased = true;
        }
    }

    public static void ClearEdges()
    {
        leftPressed = false;
        leftReleased = false;
    }

    private static void CountClick()
    {
        long now = Environment.TickCount64;
        bool continues = now - lastClickTime <= DoubleClickMilliseconds
                         && Vector2.Distance(windowPosition, lastClickPosition) <= DoubleClickSlack;

        clickCount = continues ? clickCount + 1 : 1;
        lastClickTime = now;
        lastClickPosition = windowPosition;
    }
}
