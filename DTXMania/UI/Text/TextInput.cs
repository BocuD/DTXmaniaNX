using DTXMania.Core;
using Hexa.NET.GLFW;

namespace DTXMania.UI.Text;

/// <summary>One thing the window reported: a character, or an editing key.</summary>
public readonly record struct TextInputEvent(string character, GlfwKey key, GlfwMod mods)
{
    public bool IsCharacter => character.Length > 0;
}

/// <summary>
/// What the window reported this frame for whoever is typing. Characters arrive already committed — an
/// IME hands over a whole phrase at once — so this is what makes Japanese input work without knowing
/// anything about the IME.
///
/// Events are recorded from the GLFW callbacks during polling and read once per frame by whoever holds
/// focus. Anything nobody read is dropped rather than carried over.
/// </summary>
public static class TextInput
{
    private static readonly List<TextInputEvent> recorded = [];

    public static IReadOnlyList<TextInputEvent> events => recorded;

    public static void Typed(uint codepoint)
    {
        //below space is a control code, which arrives as a key rather than as something to insert
        if (codepoint >= ' ')
        {
            recorded.Add(new TextInputEvent(char.ConvertFromUtf32((int)codepoint), GlfwKey.Unknown, default));
        }
    }

    /// <summary>Records a press or an OS repeat, which is what makes a held backspace keep deleting.</summary>
    public static void KeyPressed(GlfwKey key, GlfwMod mods)
    {
        recorded.Add(new TextInputEvent(string.Empty, key, mods));
    }

    public static void Clear() => recorded.Clear();

    public static string GetClipboard() => CDTXMania.app?.maniaGl?.host?.GetClipboardText() ?? string.Empty;

    public static void SetClipboard(string value) => CDTXMania.app?.maniaGl?.host?.SetClipboardText(value);
}
