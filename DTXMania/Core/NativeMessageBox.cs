using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DTXMania.Core;

internal enum MessageBoxIcon
{
    Information,
    Warning,
    Error,
    Question
}

/// <summary>
/// Draw a basic messagebox using OS native APIs. Needs to be called from the main thread.
/// </summary>
internal static class NativeMessageBox
{
    public static void Show(string title, string message, MessageBoxIcon icon = MessageBoxIcon.Error)
    {
        ShowPlatform(title, message, icon, yesNo: false);
    }

    /// <summary>Returns false if no dialog could be shown.</summary>
    public static bool Ask(string title, string message, MessageBoxIcon icon = MessageBoxIcon.Question)
    {
        return ShowPlatform(title, message, icon, yesNo: true);
    }

    private static bool ShowPlatform(string title, string message, MessageBoxIcon icon, bool yesNo)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return Win32.Show(title, message, icon, yesNo);
            }

            if (OperatingSystem.IsMacOS() && AppKit.IsMainThread())
            {
                return AppKit.Show(title, message, icon, yesNo);
            }

            if (OperatingSystem.IsLinux() && Linux.TryShow(title, message, icon, yesNo, out bool result))
            {
                return result;
            }
        }
        catch (Exception e)
        {
            Trace.TraceError("Failed to show message box: " + e);
        }

        Console.Error.WriteLine($"{title}: {message}");
        return false;
    }

    private static bool IsJapanese => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja";

    private static class Win32
    {
        private const uint MB_OK = 0x0;
        private const uint MB_YESNO = 0x4;
        private const uint MB_ICONERROR = 0x10;
        private const uint MB_ICONQUESTION = 0x20;
        private const uint MB_ICONWARNING = 0x30;
        private const uint MB_ICONINFORMATION = 0x40;
        private const uint MB_SETFOREGROUND = 0x10000;
        private const uint MB_TOPMOST = 0x40000;
        private const int IDYES = 6;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        public static bool Show(string title, string message, MessageBoxIcon icon, bool yesNo)
        {
            uint type = (yesNo ? MB_YESNO : MB_OK) | MB_SETFOREGROUND | MB_TOPMOST | icon switch
            {
                MessageBoxIcon.Information => MB_ICONINFORMATION,
                MessageBoxIcon.Warning => MB_ICONWARNING,
                MessageBoxIcon.Question => MB_ICONQUESTION,
                _ => MB_ICONERROR
            };

            return MessageBoxW(IntPtr.Zero, message, title, type) == IDYES;
        }
    }

}
