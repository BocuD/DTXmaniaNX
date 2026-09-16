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

    //thank you claude
    private static class AppKit
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        private const string AppKitPath = "/System/Library/Frameworks/AppKit.framework/AppKit";

        private const long NSAlertStyleWarning = 0;
        private const long NSAlertStyleInformational = 1;
        private const long NSAlertStyleCritical = 2;
        private const long NSApplicationActivationPolicyRegular = 0;
        private const long NSAlertFirstButtonReturn = 1000;

        [DllImport("/usr/lib/libSystem.dylib")]
        private static extern int pthread_main_np();

        [DllImport(ObjC)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(ObjC)]
        private static extern IntPtr sel_registerName(string name);

        [DllImport(ObjC)]
        private static extern IntPtr objc_autoreleasePoolPush();

        [DllImport(ObjC)]
        private static extern void objc_autoreleasePoolPop(IntPtr pool);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector, long arg);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string arg);

        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        private static extern long SendLong(IntPtr receiver, IntPtr selector);

        public static bool IsMainThread() => pthread_main_np() == 1;

        public static bool Show(string title, string message, MessageBoxIcon icon, bool yesNo)
        {
            //may run before GLFW has loaded AppKit
            NativeLibrary.Load(AppKitPath);

            IntPtr pool = objc_autoreleasePoolPush();
            try
            {
                //an unbundled executable can't take focus otherwise, leaving the alert behind other windows
                IntPtr app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
                Send(app, sel_registerName("setActivationPolicy:"), NSApplicationActivationPolicyRegular);
                Send(app, sel_registerName("activateIgnoringOtherApps:"), 1);

                IntPtr alert = Send(Send(objc_getClass("NSAlert"), sel_registerName("alloc")), sel_registerName("init"));
                Send(alert, sel_registerName("setMessageText:"), NSString(title));
                Send(alert, sel_registerName("setInformativeText:"), NSString(message));
                Send(alert, sel_registerName("setAlertStyle:"), icon switch
                {
                    MessageBoxIcon.Error => NSAlertStyleCritical,
                    MessageBoxIcon.Warning => NSAlertStyleWarning,
                    _ => NSAlertStyleInformational
                });

                if (yesNo)
                {
                    AddButton(alert, IsJapanese ? "はい" : "Yes");
                    IntPtr no = AddButton(alert, IsJapanese ? "いいえ" : "No");
                    //AppKit only binds Escape to buttons titled Cancel
                    Send(no, sel_registerName("setKeyEquivalent:"), NSString("\u001b"));
                }
                else
                {
                    AddButton(alert, "OK");
                }

                long response = SendLong(alert, sel_registerName("runModal"));
                Send(alert, sel_registerName("release"));

                return yesNo && response == NSAlertFirstButtonReturn;
            }
            finally
            {
                objc_autoreleasePoolPop(pool);
            }
        }

        private static IntPtr AddButton(IntPtr alert, string title) =>
            Send(alert, sel_registerName("addButtonWithTitle:"), NSString(title));

        //autoreleased; valid until Show pops its pool
        private static IntPtr NSString(string value) =>
            Send(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), value);
    }
}
